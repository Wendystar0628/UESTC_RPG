<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRBodyTracker

命名空间 `Godot`；class；继承 [`XRPositionalTracker`](XRPositionalTracker.md) → [`XRTracker`](XRTracker.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A body tracking system will create an instance of this object and add it to the [`XRServer`](XRServer.md). This tracking system will then obtain skeleton data, convert it to the Godot Humanoid skeleton and store this data on the `XRBodyTracker` object.

Use [`XRBodyModifier3D`](XRBodyModifier3D.md) to animate a body mesh using body tracking data.

## 构造函数

### XRBodyTracker

```csharp
public XRBodyTracker()
```

## 属性

### BodyFlags

```csharp
public XRBodyTracker.BodyFlagsEnum BodyFlags { get; set; }
```

引擎名：`body_flags`

The type of body tracking data captured.

### HasTrackingData

```csharp
public bool HasTrackingData { get; set; }
```

引擎名：`has_tracking_data`

If `true`, the body tracking data is valid.

## 方法

### GetBodyFlags

```csharp
public XRBodyTracker.BodyFlagsEnum GetBodyFlags()
```

引擎名：`get_body_flags`

### GetHasTrackingData

```csharp
public bool GetHasTrackingData()
```

引擎名：`get_has_tracking_data`

### GetJointFlags

```csharp
public XRBodyTracker.JointFlags GetJointFlags(XRBodyTracker.Joint joint)
```

引擎名：`get_joint_flags`

Returns flags about the validity of the tracking data for the given body joint.

### GetJointTransform

```csharp
public Transform3D GetJointTransform(XRBodyTracker.Joint joint)
```

引擎名：`get_joint_transform`

Returns the transform for the given body joint.

### SetBodyFlags

```csharp
public void SetBodyFlags(XRBodyTracker.BodyFlagsEnum flags)
```

引擎名：`set_body_flags`

### SetHasTrackingData

```csharp
public void SetHasTrackingData(bool hasData)
```

引擎名：`set_has_tracking_data`

### SetJointFlags

```csharp
public void SetJointFlags(XRBodyTracker.Joint joint, XRBodyTracker.JointFlags flags)
```

引擎名：`set_joint_flags`

Sets flags about the validity of the tracking data for the given body joint.

### SetJointTransform

```csharp
public void SetJointTransform(XRBodyTracker.Joint joint, Transform3D transform)
```

引擎名：`set_joint_transform`

Sets the transform for the given body joint.

## BodyFlagsEnum

命名空间 `Godot`；enum

### 枚举值

- `UpperBodySupported = 1`：Upper body tracking supported.
- `LowerBodySupported = 2`：Lower body tracking supported.
- `HandsSupported = 4`：Hand tracking supported.

## Joint

命名空间 `Godot`；enum

### 枚举值

- `Root = 0`：Root joint.
- `Hips = 1`：Hips joint.
- `Spine = 2`：Spine joint.
- `Chest = 3`：Chest joint.
- `UpperChest = 4`：Upper chest joint.
- `Neck = 5`：Neck joint.
- `Head = 6`：Head joint.
- `HeadTip = 7`：Head tip joint.
- `LeftShoulder = 8`：Left shoulder joint.
- `LeftUpperArm = 9`：Left upper arm joint.
- `LeftLowerArm = 10`：Left lower arm joint.
- `RightShoulder = 11`：Right shoulder joint.
- `RightUpperArm = 12`：Right upper arm joint.
- `RightLowerArm = 13`：Right lower arm joint.
- `LeftUpperLeg = 14`：Left upper leg joint.
- `LeftLowerLeg = 15`：Left lower leg joint.
- `LeftFoot = 16`：Left foot joint.
- `LeftToes = 17`：Left toes joint.
- `RightUpperLeg = 18`：Right upper leg joint.
- `RightLowerLeg = 19`：Right lower leg joint.
- `RightFoot = 20`：Right foot joint.
- `RightToes = 21`：Right toes joint.
- `LeftHand = 22`：Left hand joint.
- `LeftPalm = 23`：Left palm joint.
- `LeftWrist = 24`：Left wrist joint.
- `LeftThumbMetacarpal = 25`：Left thumb metacarpal joint.
- `LeftThumbPhalanxProximal = 26`：Left thumb phalanx proximal joint.
- `LeftThumbPhalanxDistal = 27`：Left thumb phalanx distal joint.
- `LeftThumbTip = 28`：Left thumb tip joint.
- `LeftIndexFingerMetacarpal = 29`：Left index finger metacarpal joint.
- `LeftIndexFingerPhalanxProximal = 30`：Left index finger phalanx proximal joint.
- `LeftIndexFingerPhalanxIntermediate = 31`：Left index finger phalanx intermediate joint.
- `LeftIndexFingerPhalanxDistal = 32`：Left index finger phalanx distal joint.
- `LeftIndexFingerTip = 33`：Left index finger tip joint.
- `LeftMiddleFingerMetacarpal = 34`：Left middle finger metacarpal joint.
- `LeftMiddleFingerPhalanxProximal = 35`：Left middle finger phalanx proximal joint.
- `LeftMiddleFingerPhalanxIntermediate = 36`：Left middle finger phalanx intermediate joint.
- `LeftMiddleFingerPhalanxDistal = 37`：Left middle finger phalanx distal joint.
- `LeftMiddleFingerTip = 38`：Left middle finger tip joint.
- `LeftRingFingerMetacarpal = 39`：Left ring finger metacarpal joint.
- `LeftRingFingerPhalanxProximal = 40`：Left ring finger phalanx proximal joint.
- `LeftRingFingerPhalanxIntermediate = 41`：Left ring finger phalanx intermediate joint.
- `LeftRingFingerPhalanxDistal = 42`：Left ring finger phalanx distal joint.
- `LeftRingFingerTip = 43`：Left ring finger tip joint.
- `LeftPinkyFingerMetacarpal = 44`：Left pinky finger metacarpal joint.
- `LeftPinkyFingerPhalanxProximal = 45`：Left pinky finger phalanx proximal joint.
- `LeftPinkyFingerPhalanxIntermediate = 46`：Left pinky finger phalanx intermediate joint.
- `LeftPinkyFingerPhalanxDistal = 47`：Left pinky finger phalanx distal joint.
- `LeftPinkyFingerTip = 48`：Left pinky finger tip joint.
- `RightHand = 49`：Right hand joint.
- `RightPalm = 50`：Right palm joint.
- `RightWrist = 51`：Right wrist joint.
- `RightThumbMetacarpal = 52`：Right thumb metacarpal joint.
- `RightThumbPhalanxProximal = 53`：Right thumb phalanx proximal joint.
- `RightThumbPhalanxDistal = 54`：Right thumb phalanx distal joint.
- `RightThumbTip = 55`：Right thumb tip joint.
- `RightIndexFingerMetacarpal = 56`：Right index finger metacarpal joint.
- `RightIndexFingerPhalanxProximal = 57`：Right index finger phalanx proximal joint.
- `RightIndexFingerPhalanxIntermediate = 58`：Right index finger phalanx intermediate joint.
- `RightIndexFingerPhalanxDistal = 59`：Right index finger phalanx distal joint.
- `RightIndexFingerTip = 60`：Right index finger tip joint.
- `RightMiddleFingerMetacarpal = 61`：Right middle finger metacarpal joint.
- `RightMiddleFingerPhalanxProximal = 62`：Right middle finger phalanx proximal joint.
- `RightMiddleFingerPhalanxIntermediate = 63`：Right middle finger phalanx intermediate joint.
- `RightMiddleFingerPhalanxDistal = 64`：Right middle finger phalanx distal joint.
- `RightMiddleFingerTip = 65`：Right middle finger tip joint.
- `RightRingFingerMetacarpal = 66`：Right ring finger metacarpal joint.
- `RightRingFingerPhalanxProximal = 67`：Right ring finger phalanx proximal joint.
- `RightRingFingerPhalanxIntermediate = 68`：Right ring finger phalanx intermediate joint.
- `RightRingFingerPhalanxDistal = 69`：Right ring finger phalanx distal joint.
- `RightRingFingerTip = 70`：Right ring finger tip joint.
- `RightPinkyFingerMetacarpal = 71`：Right pinky finger metacarpal joint.
- `RightPinkyFingerPhalanxProximal = 72`：Right pinky finger phalanx proximal joint.
- `RightPinkyFingerPhalanxIntermediate = 73`：Right pinky finger phalanx intermediate joint.
- `RightPinkyFingerPhalanxDistal = 74`：Right pinky finger phalanx distal joint.
- `RightPinkyFingerTip = 75`：Right pinky finger tip joint.
- `LowerChest = 76`：Lower chest joint.
- `LeftScapula = 77`：Left scapula joint.
- `LeftWristTwist = 78`：Left wrist twist joint.
- `RightScapula = 79`：Right scapula joint.
- `RightWristTwist = 80`：Right wrist twist joint.
- `LeftFootTwist = 81`：Left foot twist joint.
- `LeftHeel = 82`：Left heel joint.
- `LeftMiddleFoot = 83`：Left middle foot joint.
- `RightFootTwist = 84`：Right foot twist joint.
- `RightHeel = 85`：Right heel joint.
- `RightMiddleFoot = 86`：Right middle foot joint.
- `Max = 87`：Represents the size of the `XRBodyTracker.Joint` enum.

## JointFlags

命名空间 `Godot`；enum

### 枚举值

- `OrientationValid = 1`：The joint's orientation data is valid.
- `OrientationTracked = 2`：The joint's orientation is actively tracked.
- `PositionValid = 4`：The joint's position data is valid.
- `PositionTracked = 8`：The joint's position is actively tracked.
