@tool
extends EditorPlugin
## Build import: keep Godot's initial scan, then refresh stale RD includes.
## Usage: --headless --editor --path <game> -- --build-import.

var _failed := false
var _imported: Dictionary = {}
var _includes := RegEx.new()


func _enter_tree() -> void:
	if not OS.get_cmdline_user_args().has("--build-import"):
		return
	var filesystem := EditorInterface.get_resource_filesystem()
	if filesystem == null:
		_fail("EditorFileSystem is unavailable.")
		return
	filesystem.resources_reimported.connect(_record_imported)
	if _includes.compile('(?m)^\\s*#\\s*include\\s+"([^"\\r\\n]+)"') != OK:
		_fail("Cannot parse shader include directives.")
		return
	_finish_import.call_deferred(filesystem)


func _record_imported(paths: PackedStringArray) -> void:
	for path in paths:
		_imported[path] = true


func _finish_import(filesystem: EditorFileSystem) -> void:
	# EditorFileSystem.is_scanning includes the pending initial scan.
	# Keep the editor's own SceneTree and wait until its scan/import is complete.
	await get_tree().process_frame
	while filesystem.is_scanning() or filesystem.is_importing():
		await get_tree().process_frame
	var directory := filesystem.get_filesystem_path("res://style")
	if directory == null:
		_fail("Missing shader resource directory: res://style")
		return
	var sources: PackedStringArray = []
	_collect_sources(directory, sources)
	if sources.is_empty():
		_fail("No RD shader sources indexed under res://style.")
		return
	var stale: PackedStringArray = []
	for source in sources:
		var newest := _dependency_time(source)
		if _failed:
			return
		var remap := ConfigFile.new()
		var error := remap.load(source + ".import")
		if error != OK:
			_fail("Cannot read %s.import: %s" % [source, error_string(error)])
			return
		var output: String = remap.get_value("remap", "path", "")
		if output.is_empty() or not FileAccess.file_exists(output):
			_fail("Missing imported RD shader: %s (%s)" % [source, output])
			return
		# FileAccess exposes integer seconds; equality is conservatively stale.
		# Sources imported by the initial scan already contain the current includes.
		if not _imported.has(source) and newest >= FileAccess.get_modified_time(output):
			stale.append(source)
	if not stale.is_empty():
		print("Reimport RD shader includes: ", stale)
		filesystem.reimport_files(stale)
	for source in sources:
		if not _shader_valid(source):
			return
	get_tree().quit(0)


func _collect_sources(directory: EditorFileSystemDirectory, sources: PackedStringArray) -> void:
	# Use the editor's existing style index; do not scan repository files again.
	for index in directory.get_file_count():
		var path := directory.get_file_path(index)
		if path.get_extension() == "glsl":
			sources.append(path)
	for index in directory.get_subdir_count():
		_collect_sources(directory.get_subdir(index), sources)


func _dependency_time(source: String) -> int:
	var pending: Array[String] = [source]
	var seen: Dictionary = {}
	var newest: int = 0
	while not pending.is_empty():
		var path: String = pending.pop_back()
		if seen.has(path):
			continue
		seen[path] = true
		var file := FileAccess.open(path, FileAccess.READ)
		if file == null:
			_fail("Cannot read shader input %s: %s" % [path, error_string(FileAccess.get_open_error())])
			return 0
		var length := file.get_length()
		# get_as_text restores the original cursor, so verify the bytes read instead.
		var bytes := file.get_buffer(length)
		var error := file.get_error()
		file.close()
		if bytes.size() != length or (error != OK and error != ERR_FILE_EOF):
			_fail("Cannot read shader input %s: %s (%d/%d bytes)" % [path, error_string(error), bytes.size(), length])
			return 0
		var text := bytes.get_string_from_utf8()
		newest = maxi(newest, FileAccess.get_modified_time(path))
		for match in _includes.search_all(text):
			var include: String = match.get_string(1)
			# ResourceImporterShaderFile resolves every relative include from the
			# RD main source directory, including directives in nested includes.
			if include.is_relative_path():
				include = source.get_base_dir().path_join(include)
			pending.append(include.simplify_path())
	return newest


func _shader_valid(source: String) -> bool:
	# The official importer saves RDShaderFile and returns OK even on GLSL errors.
	var shader := ResourceLoader.load(source, "RDShaderFile", ResourceLoader.CACHE_MODE_IGNORE) as RDShaderFile
	if shader == null:
		_fail("Cannot load imported RD shader: %s" % source)
		return false
	if not shader.base_error.is_empty():
		_fail("%s: %s" % [source, shader.base_error])
		return false
	var versions := shader.get_version_list()
	if versions.is_empty():
		_fail("Imported RD shader has no compiled versions: %s" % source)
		return false
	for version in versions:
		var spirv := shader.get_spirv(version)
		if spirv == null:
			_fail("Missing RD shader version: %s [%s]" % [source, version])
			return false
		for stage in RenderingDevice.SHADER_STAGE_MAX:
			var error := spirv.get_stage_compile_error(stage)
			if not error.is_empty():
				_fail("%s [%s]: %s" % [source, version, error])
				return false
	return true


func _fail(message: String) -> void:
	_failed = true
	push_error(message)
	get_tree().quit(1)
