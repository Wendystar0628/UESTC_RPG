<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# InputEventMidi

命名空间 `Godot`；class；继承 [`InputEvent`](InputEvent.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

InputEventMIDI stores information about messages from [MIDI](https://en.wikipedia.org/wiki/MIDI) (Musical Instrument Digital Interface) devices. These may include musical keyboards, synthesizers, and drum machines.

MIDI messages can be received over a 5-pin MIDI connector or over USB. If your device supports both be sure to check the settings in the device to see which output it is using.

By default, Godot does not detect MIDI devices. You need to call [`OS.OpenMidiInputs`](OS.md), first. You can check which devices are detected with [`OS.GetConnectedMidiInputs`](OS.md), and close the connection with [`OS.CloseMidiInputs`](OS.md).

```csharp
public override void _Ready()
{
OS.OpenMidiInputs();
GD.Print(OS.GetConnectedMidiInputs());
}

public override void _Input(InputEvent inputEvent)
{
if (inputEvent is InputEventMidi midiEvent)
{
PrintMIDIInfo(midiEvent);
}
}

private void PrintMIDIInfo(InputEventMidi midiEvent)
{
GD.Print(midiEvent);
GD.Print($"Channel {midiEvent.Channel}");
GD.Print($"Message {midiEvent.Message}");
GD.Print($"Pitch {midiEvent.Pitch}");
GD.Print($"Velocity {midiEvent.Velocity}");
GD.Print($"Instrument {midiEvent.Instrument}");
GD.Print($"Pressure {midiEvent.Pressure}");
GD.Print($"Controller number: {midiEvent.ControllerNumber}");
GD.Print($"Controller value: {midiEvent.ControllerValue}");
}

```

**Note:** Godot does not support MIDI output, so there is no way to emit MIDI messages from Godot. Only MIDI input is supported.

**Note:** On the Web platform, using MIDI input requires a browser permission to be granted first. This permission request is performed when calling [`OS.OpenMidiInputs`](OS.md). MIDI input will not work until the user accepts the permission request.

## 构造函数

### InputEventMidi

```csharp
public InputEventMidi()
```

## 属性

### Channel

```csharp
public int Channel { get; set; }
```

The MIDI channel of this message, ranging from `0` to `15`. MIDI channel `9` is reserved for percussion instruments.

### ControllerNumber

```csharp
public int ControllerNumber { get; set; }
```

The unique number of the controller, if `Message` is [`MidiMessage.ControlChange`](MidiMessage.md), otherwise this is `0`. This value can be used to identify sliders for volume, balance, and panning, as well as switches and pedals on the MIDI device. See the [General MIDI specification](https://en.wikipedia.org/wiki/General_MIDI#Controller_events) for a small list.

### ControllerValue

```csharp
public int ControllerValue { get; set; }
```

The value applied to the controller. If `Message` is [`MidiMessage.ControlChange`](MidiMessage.md), this value ranges from `0` to `127`, otherwise it is `0`. See also `ControllerValue`.

### Instrument

```csharp
public int Instrument { get; set; }
```

The instrument (also called *program* or *preset*) used on this MIDI message. This value ranges from `0` to `127`.

To see what each value means, refer to the [General MIDI's instrument list](https://en.wikipedia.org/wiki/General_MIDI#Program_change_events). Keep in mind that the list is off by 1 because it does not begin from 0. A value of `0` corresponds to the acoustic grand piano.

### Message

```csharp
public MidiMessage Message { get; set; }
```

Represents the type of MIDI message (see the [`MidiMessage`](MidiMessage.md) enum).

For more information, see the [MIDI message status byte list chart](https://www.midi.org/specifications-old/item/table-2-expanded-messages-list-status-bytes).

### Pitch

```csharp
public int Pitch { get; set; }
```

The pitch index number of this MIDI message. This value ranges from `0` to `127`.

On a piano, the **middle C** is `60`, followed by a **C-sharp** (`61`), then a **D** (`62`), and so on. Each octave is split in offsets of 12. See the "MIDI note number" column of the [piano key frequency chart](https://en.wikipedia.org/wiki/Piano_key_frequencies) a full list.

### Pressure

```csharp
public int Pressure { get; set; }
```

The strength of the key being pressed. This value ranges from `0` to `127`.

**Note:** For many devices, this value is always `0`. Other devices such as musical keyboards may simulate pressure by changing the `Velocity`, instead.

### Velocity

```csharp
public int Velocity { get; set; }
```

The velocity of the MIDI message. This value ranges from `0` to `127`. For a musical keyboard, this corresponds to how quickly the key was pressed, and is rarely above `110` in practice.

**Note:** Some MIDI devices may send a [`MidiMessage.NoteOn`](MidiMessage.md) message with `0` velocity and expect it to be treated the same as a [`MidiMessage.NoteOff`](MidiMessage.md) message. If necessary, this can be handled with a few lines of code:

```csharp
func _input(event):
if event is InputEventMIDI:
if event.message == MIDI_MESSAGE_NOTE_ON and event.velocity > 0:
print("Note pressed!")

```

## 方法

### GetChannel

```csharp
public int GetChannel()
```

### GetControllerNumber

```csharp
public int GetControllerNumber()
```

### GetControllerValue

```csharp
public int GetControllerValue()
```

### GetInstrument

```csharp
public int GetInstrument()
```

### GetMessage

```csharp
public MidiMessage GetMessage()
```

### GetPitch

```csharp
public int GetPitch()
```

### GetPressure

```csharp
public int GetPressure()
```

### GetVelocity

```csharp
public int GetVelocity()
```

### SetChannel

```csharp
public void SetChannel(int channel)
```

### SetControllerNumber

```csharp
public void SetControllerNumber(int controllerNumber)
```

### SetControllerValue

```csharp
public void SetControllerValue(int controllerValue)
```

### SetInstrument

```csharp
public void SetInstrument(int instrument)
```

### SetMessage

```csharp
public void SetMessage(MidiMessage message)
```

### SetPitch

```csharp
public void SetPitch(int pitch)
```

### SetPressure

```csharp
public void SetPressure(int pressure)
```

### SetVelocity

```csharp
public void SetVelocity(int velocity)
```
