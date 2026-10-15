<!-- 由 scripts/godot-api/gen_api_docs.py 生成，Godot Engine v4.7.2.stable.mono.official。不要手改。 -->

# SkeletonProfileHumanoid

命名空间 `Godot`；class；继承 [`SkeletonProfile`](SkeletonProfile.md) → [`Resource`](Resource.md) → [`RefCounted`](RefCounted.md) → [`GodotObject`](GodotObject.md)

A [`SkeletonProfile`](SkeletonProfile.md) as a preset that is optimized for the human form. This exists for standardization, so all parameters are read-only.

A humanoid skeleton profile contains 56 bones divided into 4 groups: `"Body"`, `"Face"`, `"LeftHand"`, and `"RightHand"`. It is structured as follows:

```csharp
Root
└─ Hips
├─ LeftUpperLeg
│  └─ LeftLowerLeg
│     └─ LeftFoot
│        └─ LeftToes
├─ RightUpperLeg
│  └─ RightLowerLeg
│     └─ RightFoot
│        └─ RightToes
└─ Spine
└─ Chest
└─ UpperChest
├─ Neck
│   └─ Head
│       ├─ Jaw
│       ├─ LeftEye
│       └─ RightEye
├─ LeftShoulder
│  └─ LeftUpperArm
│     └─ LeftLowerArm
│        └─ LeftHand
│           ├─ LeftThumbMetacarpal
│           │  └─ LeftThumbProximal
│           │    └─ LeftThumbDistal
│           ├─ LeftIndexProximal
│           │  └─ LeftIndexIntermediate
│           │    └─ LeftIndexDistal
│           ├─ LeftMiddleProximal
│           │  └─ LeftMiddleIntermediate
│           │    └─ LeftMiddleDistal
│           ├─ LeftRingProximal
│           │  └─ LeftRingIntermediate
│           │    └─ LeftRingDistal
│           └─ LeftLittleProximal
│              └─ LeftLittleIntermediate
│                └─ LeftLittleDistal
└─ RightShoulder
└─ RightUpperArm
└─ RightLowerArm
└─ RightHand
├─ RightThumbMetacarpal
│  └─ RightThumbProximal
│     └─ RightThumbDistal
├─ RightIndexProximal
│  └─ RightIndexIntermediate
│     └─ RightIndexDistal
├─ RightMiddleProximal
│  └─ RightMiddleIntermediate
│     └─ RightMiddleDistal
├─ RightRingProximal
│  └─ RightRingIntermediate
│     └─ RightRingDistal
└─ RightLittleProximal
└─ RightLittleIntermediate
└─ RightLittleDistal

```

## 构造函数

### SkeletonProfileHumanoid

```csharp
public SkeletonProfileHumanoid()
```
