<!-- 由 scripts/godot-api/gen-api-docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# XRFaceTracker

命名空间 `Godot`；class；继承 [`XRTracker`](XRTracker.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

An instance of this object represents a tracked face and its corresponding blend shapes. The blend shapes come from the [Unified Expressions](https://docs.vrcft.io/docs/tutorial-avatars/tutorial-avatars-extras/unified-blendshapes) standard, and contain extended details and visuals for each blend shape. Additionally the [Tracking Standard Comparison](https://docs.vrcft.io/docs/tutorial-avatars/tutorial-avatars-extras/compatibility/overview) page documents the relationship between Unified Expressions and other standards.

As face trackers are turned on they are registered with the [`XRServer`](XRServer.md).

## 构造函数

### XRFaceTracker

```csharp
public XRFaceTracker()
```

## 属性

### BlendShapes

```csharp
public float[] BlendShapes { get; set; }
```

引擎名：`blend_shapes`

The array of face blend shape weights with indices corresponding to the [`XRFaceTracker.BlendShapeEntry`](XRFaceTracker.md) enum.

## 方法

### GetBlendShape

```csharp
public float GetBlendShape(XRFaceTracker.BlendShapeEntry blendShape)
```

引擎名：`get_blend_shape`

Returns the requested face blend shape weight.

### GetBlendShapes

```csharp
public float[] GetBlendShapes()
```

引擎名：`get_blend_shapes`

### SetBlendShape

```csharp
public void SetBlendShape(XRFaceTracker.BlendShapeEntry blendShape, float weight)
```

引擎名：`set_blend_shape`

Sets a face blend shape weight.

### SetBlendShapes

```csharp
public void SetBlendShapes(ReadOnlySpan<float> weights)
```

引擎名：`set_blend_shapes`

### SetBlendShapes

```csharp
public void SetBlendShapes(float[] weights)
```

引擎名：`set_blend_shapes`

## BlendShapeEntry

命名空间 `Godot`；enum

### 枚举值

- `EyeLookOutRight = 0`：Right eye looks outwards.
- `EyeLookInRight = 1`：Right eye looks inwards.
- `EyeLookUpRight = 2`：Right eye looks upwards.
- `EyeLookDownRight = 3`：Right eye looks downwards.
- `EyeLookOutLeft = 4`：Left eye looks outwards.
- `EyeLookInLeft = 5`：Left eye looks inwards.
- `EyeLookUpLeft = 6`：Left eye looks upwards.
- `EyeLookDownLeft = 7`：Left eye looks downwards.
- `EyeClosedRight = 8`：Closes the right eyelid.
- `EyeClosedLeft = 9`：Closes the left eyelid.
- `EyeSquintRight = 10`：Squeezes the right eye socket muscles.
- `EyeSquintLeft = 11`：Squeezes the left eye socket muscles.
- `EyeWideRight = 12`：Right eyelid widens beyond relaxed.
- `EyeWideLeft = 13`：Left eyelid widens beyond relaxed.
- `EyeDilationRight = 14`：Dilates the right eye pupil.
- `EyeDilationLeft = 15`：Dilates the left eye pupil.
- `EyeConstrictRight = 16`：Constricts the right eye pupil.
- `EyeConstrictLeft = 17`：Constricts the left eye pupil.
- `BrowPinchRight = 18`：Right eyebrow pinches in.
- `BrowPinchLeft = 19`：Left eyebrow pinches in.
- `BrowLowererRight = 20`：Outer right eyebrow pulls down.
- `BrowLowererLeft = 21`：Outer left eyebrow pulls down.
- `BrowInnerUpRight = 22`：Inner right eyebrow pulls up.
- `BrowInnerUpLeft = 23`：Inner left eyebrow pulls up.
- `BrowOuterUpRight = 24`：Outer right eyebrow pulls up.
- `BrowOuterUpLeft = 25`：Outer left eyebrow pulls up.
- `NoseSneerRight = 26`：Right side face sneers.
- `NoseSneerLeft = 27`：Left side face sneers.
- `NasalDilationRight = 28`：Right side nose canal dilates.
- `NasalDilationLeft = 29`：Left side nose canal dilates.
- `NasalConstrictRight = 30`：Right side nose canal constricts.
- `NasalConstrictLeft = 31`：Left side nose canal constricts.
- `CheekSquintRight = 32`：Raises the right side cheek.
- `CheekSquintLeft = 33`：Raises the left side cheek.
- `CheekPuffRight = 34`：Puffs the right side cheek.
- `CheekPuffLeft = 35`：Puffs the left side cheek.
- `CheekSuckRight = 36`：Sucks in the right side cheek.
- `CheekSuckLeft = 37`：Sucks in the left side cheek.
- `JawOpen = 38`：Opens jawbone.
- `MouthClosed = 39`：Closes the mouth.
- `JawRight = 40`：Pushes jawbone right.
- `JawLeft = 41`：Pushes jawbone left.
- `JawForward = 42`：Pushes jawbone forward.
- `JawBackward = 43`：Pushes jawbone backward.
- `JawClench = 44`：Flexes jaw muscles.
- `JawMandibleRaise = 45`：Raises the jawbone.
- `LipSuckUpperRight = 46`：Upper right lip part tucks in the mouth.
- `LipSuckUpperLeft = 47`：Upper left lip part tucks in the mouth.
- `LipSuckLowerRight = 48`：Lower right lip part tucks in the mouth.
- `LipSuckLowerLeft = 49`：Lower left lip part tucks in the mouth.
- `LipSuckCornerRight = 50`：Right lip corner folds into the mouth.
- `LipSuckCornerLeft = 51`：Left lip corner folds into the mouth.
- `LipFunnelUpperRight = 52`：Upper right lip part pushes into a funnel.
- `LipFunnelUpperLeft = 53`：Upper left lip part pushes into a funnel.
- `LipFunnelLowerRight = 54`：Lower right lip part pushes into a funnel.
- `LipFunnelLowerLeft = 55`：Lower left lip part pushes into a funnel.
- `LipPuckerUpperRight = 56`：Upper right lip part pushes outwards.
- `LipPuckerUpperLeft = 57`：Upper left lip part pushes outwards.
- `LipPuckerLowerRight = 58`：Lower right lip part pushes outwards.
- `LipPuckerLowerLeft = 59`：Lower left lip part pushes outwards.
- `MouthUpperUpRight = 60`：Upper right part of the lip pulls up.
- `MouthUpperUpLeft = 61`：Upper left part of the lip pulls up.
- `MouthLowerDownRight = 62`：Lower right part of the lip pulls up.
- `MouthLowerDownLeft = 63`：Lower left part of the lip pulls up.
- `MouthUpperDeepenRight = 64`：Upper right lip part pushes in the cheek.
- `MouthUpperDeepenLeft = 65`：Upper left lip part pushes in the cheek.
- `MouthUpperRight = 66`：Moves upper lip right.
- `MouthUpperLeft = 67`：Moves upper lip left.
- `MouthLowerRight = 68`：Moves lower lip right.
- `MouthLowerLeft = 69`：Moves lower lip left.
- `MouthCornerPullRight = 70`：Right lip corner pulls diagonally up and out.
- `MouthCornerPullLeft = 71`：Left lip corner pulls diagonally up and out.
- `MouthCornerSlantRight = 72`：Right corner lip slants up.
- `MouthCornerSlantLeft = 73`：Left corner lip slants up.
- `MouthFrownRight = 74`：Right corner lip pulls down.
- `MouthFrownLeft = 75`：Left corner lip pulls down.
- `MouthStretchRight = 76`：Mouth corner lip pulls out and down.
- `MouthStretchLeft = 77`：Mouth corner lip pulls out and down.
- `MouthDimpleRight = 78`：Right lip corner is pushed backwards.
- `MouthDimpleLeft = 79`：Left lip corner is pushed backwards.
- `MouthRaiserUpper = 80`：Raises and slightly pushes out the upper mouth.
- `MouthRaiserLower = 81`：Raises and slightly pushes out the lower mouth.
- `MouthPressRight = 82`：Right side lips press and flatten together vertically.
- `MouthPressLeft = 83`：Left side lips press and flatten together vertically.
- `MouthTightenerRight = 84`：Right side lips squeeze together horizontally.
- `MouthTightenerLeft = 85`：Left side lips squeeze together horizontally.
- `TongueOut = 86`：Tongue visibly sticks out of the mouth.
- `TongueUp = 87`：Tongue points upwards.
- `TongueDown = 88`：Tongue points downwards.
- `TongueRight = 89`：Tongue points right.
- `TongueLeft = 90`：Tongue points left.
- `TongueRoll = 91`：Sides of the tongue funnel, creating a roll.
- `TongueBlendDown = 92`：Tongue arches up then down inside the mouth.
- `TongueCurlUp = 93`：Tongue arches down then up inside the mouth.
- `TongueSquish = 94`：Tongue squishes together and thickens.
- `TongueFlat = 95`：Tongue flattens and thins out.
- `TongueTwistRight = 96`：Tongue tip rotates clockwise, with the rest following gradually.
- `TongueTwistLeft = 97`：Tongue tip rotates counter-clockwise, with the rest following gradually.
- `SoftPalateClose = 98`：Inner mouth throat closes.
- `ThroatSwallow = 99`：The Adam's apple visibly swallows.
- `NeckFlexRight = 100`：Right side neck visibly flexes.
- `NeckFlexLeft = 101`：Left side neck visibly flexes.
- `EyeClosed = 102`：Closes both eye lids.
- `EyeWide = 103`：Widens both eye lids.
- `EyeSquint = 104`：Squints both eye lids.
- `EyeDilation = 105`：Dilates both pupils.
- `EyeConstrict = 106`：Constricts both pupils.
- `BrowDownRight = 107`：Pulls the right eyebrow down and in.
- `BrowDownLeft = 108`：Pulls the left eyebrow down and in.
- `BrowDown = 109`：Pulls both eyebrows down and in.
- `BrowUpRight = 110`：Right brow appears worried.
- `BrowUpLeft = 111`：Left brow appears worried.
- `BrowUp = 112`：Both brows appear worried.
- `NoseSneer = 113`：Entire face sneers.
- `NasalDilation = 114`：Both nose canals dilate.
- `NasalConstrict = 115`：Both nose canals constrict.
- `CheekPuff = 116`：Puffs both cheeks.
- `CheekSuck = 117`：Sucks in both cheeks.
- `CheekSquint = 118`：Raises both cheeks.
- `LipSuckUpper = 119`：Tucks in the upper lips.
- `LipSuckLower = 120`：Tucks in the lower lips.
- `LipSuck = 121`：Tucks in both lips.
- `LipFunnelUpper = 122`：Funnels in the upper lips.
- `LipFunnelLower = 123`：Funnels in the lower lips.
- `LipFunnel = 124`：Funnels in both lips.
- `LipPuckerUpper = 125`：Upper lip part pushes outwards.
- `LipPuckerLower = 126`：Lower lip part pushes outwards.
- `LipPucker = 127`：Lips push outwards.
- `MouthUpperUp = 128`：Raises the upper lips.
- `MouthLowerDown = 129`：Lowers the lower lips.
- `MouthOpen = 130`：Mouth opens, revealing teeth.
- `MouthRight = 131`：Moves mouth right.
- `MouthLeft = 132`：Moves mouth left.
- `MouthSmileRight = 133`：Right side of the mouth smiles.
- `MouthSmileLeft = 134`：Left side of the mouth smiles.
- `MouthSmile = 135`：Mouth expresses a smile.
- `MouthSadRight = 136`：Right side of the mouth expresses sadness.
- `MouthSadLeft = 137`：Left side of the mouth expresses sadness.
- `MouthSad = 138`：Mouth expresses sadness.
- `MouthStretch = 139`：Mouth stretches.
- `MouthDimple = 140`：Lip corners dimple.
- `MouthTightener = 141`：Mouth tightens.
- `MouthPress = 142`：Mouth presses together.
- `Max = 143`：Represents the size of the `XRFaceTracker.BlendShapeEntry` enum.
