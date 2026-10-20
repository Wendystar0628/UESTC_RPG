<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# MidiMessage

命名空间 `Godot`；enum

## 枚举值

- `None = 0`：Does not correspond to any MIDI message.
- `NoteOff = 8`：MIDI message sent when a note is released.
- `NoteOn = 9`：MIDI message sent when a note is pressed.
- `Aftertouch = 10`：MIDI message sent to indicate a change in pressure while a note is being pressed down, also called aftertouch.
- `ControlChange = 11`：MIDI message sent when a controller value changes.
- `ProgramChange = 12`：MIDI message sent when the MIDI device changes its current instrument (also called *program* or *preset*).
- `ChannelPressure = 13`：MIDI message sent to indicate a change in pressure for the whole channel.
- `PitchBend = 14`：MIDI message sent when the value of the pitch bender changes, usually a wheel on the MIDI device.
- `SystemExclusive = 240`：MIDI system exclusive (SysEx) message.
- `QuarterFrame = 241`：MIDI message sent every quarter frame to keep connected MIDI devices synchronized.
- `SongPositionPointer = 242`：MIDI message sent to jump onto a new position in the current sequence or song.
- `SongSelect = 243`：MIDI message sent to select a sequence or song to play.
- `TuneRequest = 246`：MIDI message sent to request a tuning calibration.
- `TimingClock = 248`：MIDI message sent 24 times after `QuarterFrame`, to keep connected MIDI devices synchronized.
- `Start = 250`：MIDI message sent to start the current sequence or song from the beginning.
- `Continue = 251`：MIDI message sent to resume from the point the current sequence or song was paused.
- `Stop = 252`：MIDI message sent to pause the current sequence or song.
- `ActiveSensing = 254`：MIDI message sent repeatedly while the MIDI device is idle, to tell the receiver that the connection is alive.
- `SystemReset = 255`：MIDI message sent to reset a MIDI device to its default state, as if it was just turned on.
