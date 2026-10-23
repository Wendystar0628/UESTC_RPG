@tool
extends EditorPlugin
## Build import: keep Godot's initial scan; MSBuild requests RD include reimports.
## Usage: --headless --editor --path <game> -- --build-import | --build-cloud-noise.

func _enter_tree() -> void:
	if OS.get_cmdline_user_args().has("--build-cloud-noise"):
		_build_cloud_noise.call_deferred()
		return
	if not OS.get_cmdline_user_args().has("--build-import"):
		return
	var filesystem := EditorInterface.get_resource_filesystem()
	if filesystem == null:
		_fail("EditorFileSystem is unavailable.")
		return
	_finish_import.call_deferred(filesystem)


func _build_cloud_noise() -> void:
	var filesystem := EditorInterface.get_resource_filesystem()
	if filesystem == null:
		_fail("EditorFileSystem is unavailable.")
		return
	await get_tree().process_frame
	while filesystem.is_scanning() or filesystem.is_importing():
		await get_tree().process_frame
	var script := load("res://addons/build_import/CloudNoiseBuild.cs") as CSharpScript
	if script == null or not script.can_instantiate():
		_fail("Cannot instantiate the cloud noise build tool; compile Campus first.")
		return
	var builder = script.new()
	var error = builder.Build()
	builder.Release()
	builder = null
	if error != OK:
		_fail("Cannot build cloud noise: %s" % error_string(error))
		return
	print("Built cloud noise: res://data/cloud-noise.res")
	_quit(0)


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
	if OS.get_cmdline_user_args().has("--reimport-rd-shaders"):
		print("Reimport RD shaders: ", sources)
		filesystem.reimport_files(sources)
	for source in sources:
		if not _shader_valid(source):
			return
	_quit(0)


func _collect_sources(directory: EditorFileSystemDirectory, sources: PackedStringArray) -> void:
	# Use the editor's existing style index; do not scan repository files again.
	for index in directory.get_file_count():
		var path := directory.get_file_path(index)
		if path.get_extension() == "glsl":
			sources.append(path)
	for index in directory.get_subdir_count():
		_collect_sources(directory.get_subdir(index), sources)


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
	push_error(message)
	_quit(1)


func _quit(code: int) -> void:
	# Import workers enqueue texture initialization even with the dummy renderer.
	# Its single-threaded finish does not flush these commands before destruction.
	RenderingServer.force_sync()
	get_tree().quit(code)
