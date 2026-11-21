# 강화 작업실을 Unity에서 만드는 실습 튜토리얼

## 실습 결과와 준비

이 실습에서는 빈 URP 프로젝트에 작업실을 만들고, 엔지니어의 손으로 상품을 집어 강화·판매·보관하는 게임을 완성한다. 마지막에는 Editor 도구로 같은 작업실을 재생성하고 Bootstrap 씬에서 설정 데이터를 읽어 시작한다.

[기획서](GameDesign.md)는 규칙의 기준이고 [기술 설계서](TechnicalDesign.md)는 구조와 수치의 기준이다. 이 튜토리얼은 실제 작업 순서를 제공한다. 각 장의 완료 확인을 통과한 뒤 다음 장으로 진행한다. 중간에 필요한 코드는 해당 장에 파일 전체를 수록했다. `...`로 생략한 실행 코드는 없다.

새 프로젝트에서 재현하는 것을 기준으로 한다. 이미 게임이 구현된 프로젝트에서는 같은 이름의 클래스를 추가하지 말고 기존 파일과 비교한다. 같은 namespace와 클래스 이름을 중복 작성하면 컴파일되지 않는다. 기존 Workshop과 Bootstrap을 보존하려면 별도 실습 프로젝트를 사용한다.

### 읽는 순서

| 구간 | 만드는 결과 |
| --- | --- |
| 1~3장 | 프로젝트, 모델, 설정 데이터와 상품 타입 |
| 4~6장 | 방·작업대·거치대와 1인칭 캐릭터 |
| 7~9장 | 화면 정보, 게임 규칙과 Inspector 연결 |
| 10~12장 | Bootstrap 실행, 기능 검증과 저장 확인 |
| 13~14장 | Editor 생성 도구와 실행 파일 빌드 |

## 1장 프로젝트와 폴더 준비

### Unity에서 할 작업

1. Unity Hub에서 Unity 6000.0.57f1의 Universal 3D 프로젝트를 만든다.
2. Window > Package Manager에서 Input System 1.14.2와 Unity UI 2.0.0을 확인한다. URP 기준 버전은 17.0.4다.
3. Edit > Project Settings > Player > Other Settings에서 Active Input Handling을 Input System Package 또는 Both로 설정한다. 재시작을 요구하면 재시작한다.
4. Project 창의 Assets 아래에 ReinforceGame 폴더를 만든다.
5. 그 아래에 Art, Characters, Products, Materials, Runtime, Editor, Resources, Prefabs, Scenes, Docs, Validation 폴더를 만든다.
6. File > New Scene에서 빈 씬을 만들고 `Assets/ReinforceGame/Scenes/WorkshopPractice.unity`로 저장한다.
7. Window > General > Console을 열어 둔다.

이후 코드는 Project 창에서 해당 폴더를 선택한 뒤 Create > Scripting > MonoBehaviour Script 등으로 파일을 만들고, 코드 편집기에서 기본 내용을 **파일 전체 코드로 교체**한다. Unity 버전에 따라 스크립트 생성 메뉴 명칭은 다를 수 있다. 파일명에는 `.cs`를 중복 입력하지 않는다.

**완료 확인:** 폴더와 저장된 실습 씬이 보이고 Console에 빨간 오류가 없다. 모델이 아직 없어도 정상이다.

## 2장 사용할 모델 준비

### 모델 선택

KayKit 에셋을 프로젝트에 임포트한 뒤 아래 파일을 찾는다.

| 역할 | 원본 위치와 파일 |
| --- | --- |
| 캐릭터 | `KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Characters/Engineer.prefab` |
| 망치 | `KayKit/Art/Model/UpgradeHammer.fbx` |
| 망치 텍스처 | `KayKit/Art/Model/UpgradeHammer.png`와 FBX가 실제 참조하는 팔레트 재질·텍스처 |
| 상품 | 위 캐릭터 팩의 `Prefabs/Accessories/` 아래 한손검·한손도끼·원형 방패·지팡이·단검·렌치 |

상품 파일명은 각각 `sword_1handed.prefab`, `axe_1handed.prefab`, `shield_round.prefab`, `staff.prefab`, `dagger.prefab`, `engineer_Wrench.prefab`다.

### 이동과 참조 확인

1. **Project 창에서** Engineer 프리팹을 ReinforceGame/Characters로 이동한다.
2. 망치 FBX와 대응 텍스처를 ReinforceGame/Art로 이동한다.
3. 상품 프리팹 6개를 ReinforceGame/Products로 이동한다.
4. 각 프리팹을 선택하고 우클릭 > Select Dependencies로 필요한 원본 모델·재질·텍스처·Avatar를 확인한다.
5. KayKit에 남아 있는 해당 의존 자산도 ReinforceGame/Art/Dependencies로 이동한다. 폴더별로 나누어 이름 충돌을 피한다. 서로 공유하는 의존 자산은 한 번만 이동한다.
6. 모델을 씬에 잠깐 끌어 놓아 메시와 색이 정상인지 확인한 뒤 테스트 오브젝트를 지운다.

Windows 탐색기에서 파일만 옮기거나 `.meta`를 지우지 않는다. Project 창에서 옮기면 GUID가 유지되어 원래 프리팹 참조도 유지된다. 이미 ReinforceGame 안에 정리된 프로젝트에서는 재이동하지 않는다.

**완료 확인:** Engineer와 상품 6개의 미리보기가 정상이고, 망치가 분홍색이 아니다. 분홍색이면 먼저 URP 재질과 텍스처 연결을 해결한다.

## 3장 설정과 상품 타입 작성

### 3.1 ReinforceSettings.cs

Runtime 폴더에 아래 파일을 만든다. `ItemDefinition`은 같은 파일에 두는 일반 데이터 클래스이며 별도 컴포넌트로 추가하지 않는다.

```csharp
using System;
using UnityEngine;

namespace ReinforceGame
{
    [CreateAssetMenu(menuName = "Reinforce Game/Settings")]
    public sealed class ReinforceSettings : ScriptableObject
    {
        public GameObject workshop;
        public ItemDefinition[] items;
        [Min(1)] public int firstTarget = 250;
        [Min(1)] public float targetGrowth = 1.45f;
        [Range(0, 1)] public float[] successRates = { .9f, .8f, .7f, .6f, .5f, .4f, .3f, .2f };
        [Range(0, 1)] public float[] destructionRates = { .05f, .08f, .12f, .18f, .25f, .32f, .4f, .5f };
        public float[] priceMultipliers = { 1, 1.8f, 3.2f, 5.5f, 9, 15, 25, 42, 70 };
        public int Target(int round) => Mathf.RoundToInt(firstTarget * Mathf.Pow(targetGrowth, round - 1));
        public float Chance(int level) => successRates[Mathf.Clamp(level, 0, successRates.Length - 1)];
        public float Destruction(int level) => Mathf.Min(1 - Chance(level), destructionRates[Mathf.Clamp(level, 0, destructionRates.Length - 1)]);
        public int Price(ItemDefinition item, int level) => Mathf.RoundToInt(item.basePrice * priceMultipliers[Mathf.Clamp(level, 0, priceMultipliers.Length - 1)]);
        public int MaxLevel => Mathf.Min(successRates.Length, priceMultipliers.Length - 1);
    }

    [Serializable]
    public sealed class ItemDefinition
    {
        public string id;
        public string title;
        public GameObject prefab;
        public int basePrice = 35;
        public Vector3 displayEuler;
        public Vector3 gripEuler;
        public Vector3 gripOffset;
        public Vector3 benchEuler = new Vector3(90, 0, 0);
        public float size = .65f;
    }
}
```

컴파일이 끝나면 Resources 폴더에서 Create > Reinforce Game > Settings를 선택하고 이름을 ReinforceSettings로 지정한다. 생성한 자산의 Workshop은 아직 비워 둔다.

Items Size를6으로 하고 아래처럼 입력한다. ID는 대소문자까지 동일하게 입력한다.

| index | id | title | prefab | basePrice | size |
| --- | --- | --- | --- | ---: | ---: |
| 0 | sword_1handed | Iron sword | 같은 이름의 상품 프리팹 | 35 | 0.62 |
| 1 | axe_1handed | Forged axe | 같은 이름의 상품 프리팹 | 40 | 0.62 |
| 2 | shield_round | Round shield | 같은 이름의 상품 프리팹 | 32 | 0.62 |
| 3 | staff | Arcane staff | 같은 이름의 상품 프리팹 | 50 | 0.80 |
| 4 | dagger | Steel dagger | 같은 이름의 상품 프리팹 | 25 | 0.62 |
| 5 | engineer_Wrench | Engineer wrench | 같은 이름의 상품 프리팹 | 38 | 0.62 |

모든 상품의 Display Euler와 Grip Euler는 `(0,180,0)`, Bench Euler는 `(90,0,0)`, Grip Offset은0으로 입력한다. 새 배열 요소의 필드 초기값이 원하는 값이라고 가정하지 말고 직접 확인한다. First Target=250, Target Growth=1.45와 확률·가격 배열도 코드의 초기값을 확인한다.

### 3.2 WorkshopItem.cs

Runtime 폴더에 작성한다. Dock은 상품의 부모·위치·회전을 한 번에 바꾸는 기능이다.

```csharp
using UnityEngine;

namespace ReinforceGame
{
    public sealed class WorkshopItem : MonoBehaviour
    {
        public ItemDefinition definition;
        public int level;
        public int storageSlot = -1;
        public Transform visual;
        public void Dock(Transform pivot, Vector3 euler, Vector3 offset = default)
        {
            transform.SetParent(pivot, false);
            transform.localPosition = offset;
            transform.localRotation = Quaternion.Euler(euler);
        }
    }

}
```

### 3.3 WorkshopStation.cs

Runtime 폴더에 작성한다. 하나의 스테이션 타입으로 강화판, 회수 버튼, 판매 트레이와 보관 칸을 구별한다.

```csharp
using UnityEngine;

namespace ReinforceGame
{
    public enum StationKind { Bench, Supply, Storage, Sell, NextRound }
    public sealed class WorkshopStation : MonoBehaviour
    {
        public StationKind kind;
        public int index;
        public Transform pivot;
    }
}
```

**완료 확인:** Console 오류가 없고 ReinforceSettings.asset의 Items에 상품 프리팹6개가 연결되어 있다. 아직 빈 씬을 실행해도 아무 게임이 나타나지 않는 것이 정상이다.

## 4장 방과 작업대 제작

### 4.1 기준 루트

1. Hierarchy에서 Create Empty로 Workshop을 만든다.
2. Transform의 메뉴에서 Reset한다. Position0, Rotation0, Scale1이다.
3. 아래 Cube들을 **Workshop의 자식**으로 만든다. 각각 3D Object > Cube를 만든 뒤 이름과 Transform을 입력한다.
4. 이후 표의 Position은 Workshop 기준 Local Position이다. Cube의 Rotation은 별도 지시가 없으면0이다.

| 이름 | Position | Scale |
| --- | --- | --- |
| Floor | `(0,-0.1,0)` | `(6,0.2,6)` |
| Ceiling | `(0,3.3,0)` | `(6,0.2,6)` |
| WallLeft | `(-3,1.6,0)` | `(0.2,3.2,6)` |
| WallRight | `(3,1.6,0)` | `(0.2,3.2,6)` |
| WallFront | `(0,1.6,3)` | `(6,3.2,0.2)` |
| WallBack | `(0,1.6,-3)` | `(6,3.2,0.2)` |
| BenchTop | `(0,0.94,1.5)` | `(2.6,0.18,1.2)` |
| ReinforcePlate | `(0,1.055,1.4)` | `(1.1,0.05,0.65)` |
| TakeButton | `(-0.95,1.09,1.04)` | `(0.42,0.12,0.28)` |
| SellTray | `(1,1.07,1.45)` | `(0.48,0.08,0.55)` |
| NextRoundSwitch | `(0,1.65,2.82)` | `(1.25,0.4,0.1)` |

다리는 크기 `(0.12,0.88,0.12)`인 Cube4개다. 위치는 x=-1.05 또는+1.05, y=0.44, z=1.07 또는1.93의 조합이다. 빈 오브젝트 BenchItemPivot은 Workshop 직속 자식으로 만들고 Position을 `(0,1.19,1.4)`로 지정한다. BenchTop의 자식으로 두면 천판의 Scale이 상품에 상속되므로 피한다.

### 4.2 재질

Materials 폴더에서 Create > Material로5개를 만들고 Shader를 Universal Render Pipeline/Lit으로 설정한다.

| 재질 | Base Map 색 RGB | Metallic | Smoothness | 적용 |
| --- | --- | ---: | ---: | --- |
| Charcoal | `(0.095,0.12,0.14)` | 0.1 | 0.3 | 벽·바닥·천장 |
| Oak | `(0.22,0.105,0.045)` | 0.05 | 0.3 | 천판·공급 뒤판 |
| Steel | `(0.085,0.105,0.12)` | 0.65 | 0.3 | 다리·강화판·받침 |
| Brass | `(0.6,0.32,0.085)` | 0.7 | 0.3 | TAKE |
| Teal | `(0.04,0.24,0.25)` | 0.35 | 0.3 | SELL·보관 뒤판 |

색 입력 창이0~255 범위면0~1 수치를255배하여 입력한다. 재질을 해당 Mesh Renderer의 Materials에 끌어 넣는다. 모든 Cube의 BoxCollider는 유지한다.

**완료 확인:** Scene뷰에서 밀폐된 방, 책상과3개 조작 영역이 구분된다. 밖에서 벽 때문에 내부가 안 보이면 Scene 카메라를 실내로 이동한다. 벽을 삭제하지 않는다.

## 5장 공급 진열대와 보관 거치대

### 5.1 공급 진열대

1. Workshop 아래에 빈 오브젝트 SupplyRack을 만든다. Position `(-2.25,0,0.35)`, Rotation `(0,-90,0)`, Scale1이다.
2. 그 자식 Cube Backboard를 만든다. Local Position `(0,1.5,0.2)`, Scale `(2.4,2.25,0.14)`로 설정하고 Oak를 적용한다.
3. SupplyRack 밑에 빈 오브젝트 SupplyPivot0~5를 만든다. Scale은 모두1이다.
4. 아래 로컬 좌표를 입력한다.

| index | Position |
| --- | --- |
| 0 | `(-0.73,1.95,0)` |
| 1 | `(0,1.95,0)` |
| 2 | `(0.73,1.95,0)` |
| 3 | `(-0.73,1.05,0)` |
| 4 | `(0,1.05,0)` |
| 5 | `(0.73,1.05,0)` |

피벗마다 받침 Cube를 만든다. 부모는 SupplyRack이고, 위치는 해당 피벗 좌표에 `(0,-0.3,0.07)`을 더한다. Scale은 `(0.62,0.08,0.32)`, 재질은 Steel이다. 피벗의 위치와 받침의 위치는 서로 다르다.

### 5.2 보관 거치대

SupplyRack을 복제해 StorageRack으로 바꾼다. Position `(2.25,0,0.35)`, Rotation `(0,90,0)`으로 설정한다. 피벗 이름은 StoragePivot0~5로 변경하고 뒤판 재질을 Teal로 바꾼다. 스크립트는 이름으로 슬롯을 자동 연결하지 않지만 이름을 구별해 두면 배열 연결이 쉽다.

StorageRack 아래에 SlotClick0~5 Cube를 만든다. 각 Position은 해당 피벗 좌표에 `(0,0,0.09)`를 더하고, Scale은 `(0.67,0.73,0.04)`, 재질은 Teal로 지정한다. Collider는 유지한다. 슬롯이 비어 있어도 이 면을 클릭해 보관할 수 있다.

### 5.3 스테이션 연결

아래 오브젝트에 Add Component로 WorkshopStation을 붙인다.

| 오브젝트 | Kind | Index | Pivot에 끌어 넣을 대상 |
| --- | --- | ---: | --- |
| BenchTop, ReinforcePlate | Bench | 0 | BenchItemPivot |
| TakeButton | Bench | 1 | BenchItemPivot |
| SellTray | Sell | 0 | SellTray 자신의 Transform |
| NextRoundSwitch | NextRound | 0 | 자신의 Transform |
| SlotClick0~5 | Storage | 해당0~5 | 해당 StoragePivot |
| 보관 받침6개 | Storage | 해당0~5 | 해당 StoragePivot |

공급 받침에는 Station을 붙이지 않는다. 상품이 생성되면 상품의 Collider로 집는다. 상품 모델을 씬에 미리 배치하지 않는다. 실행할 때 설정 데이터를 읽어 생성한다.

**완료 확인:** 양쪽에3열2행의 피벗이 있고, 빈 보관 칸에는 클릭 면과 올바른 index가 있다. 두 거치대의 앞면이 방 가운데를 향한다.

## 6장 엔지니어와 1인칭 카메라

### 6.1 FirstPersonRig.cs 작성

Runtime에 아래 전체 코드를 작성한다. 카메라 회전은 Update, 팔 자세는 LateUpdate에서 처리한다. 이미 Input Actions 자산이 있어도 이 코드는 자체 Action을 사용하므로 PlayerInput을 추가하지 않는다.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace ReinforceGame
{
    public sealed class FirstPersonRig : MonoBehaviour
    {
        public Camera view;
        public Transform rightUpper, rightLower, rightHand, leftUpper, leftLower, leftHand;
        public Transform hammerGrip, itemGrip;
        public Renderer leftArm;
        public bool carrying;
        public float sensitivity = .13f;
        public float strike;
        InputAction look, rotate;
        float yaw, pitch;

        void OnEnable()
        {
            look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            rotate = new InputAction("Rotate", InputActionType.Button, "<Mouse>/rightButton");
            look.Enable(); rotate.Enable();
        }
        void OnDisable()
        {
            look?.Dispose(); rotate?.Dispose();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        void Update()
        {
            bool turning = rotate.IsPressed();
            Cursor.lockState = turning ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !turning;
            if (!turning) return;
            var delta = look.ReadValue<Vector2>();
            yaw = Mathf.Clamp(yaw + delta.x * sensitivity, -105, 105);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -45, 32);
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
        }
        void LateUpdate()
        {
            if (leftArm) leftArm.gameObject.layer = carrying ? 30 : 31;
            Vector3 hammerTarget = transform.TransformPoint(new Vector3(.32f, -.32f + strike * .32f, .58f - strike * .12f));
            Vector3 itemTarget = transform.TransformPoint(new Vector3(-.32f, -.35f, .65f));
            Solve(rightUpper, rightLower, rightHand, hammerTarget, -transform.up + transform.right);
            Solve(leftUpper, leftLower, leftHand, itemTarget, -transform.up - transform.right);
            if (rightHand) rightHand.rotation = transform.rotation * Quaternion.Euler(-20 + strike * -65, 0, 0);
            if (leftHand) leftHand.rotation = transform.rotation * Quaternion.Euler(0, 0, -20);
        }
        public static void Solve(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 bend)
        {
            if (!upper || !lower || !hand) return;
            var origin = upper.position;
            float a = Vector3.Distance(origin, lower.position), b = Vector3.Distance(lower.position, hand.position);
            var direction = target - origin;
            float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
            var axis = direction.normalized;
            var perpendicular = Vector3.ProjectOnPlane(bend, axis).normalized;
            float along = (a * a + distance * distance - b * b) / (2 * distance);
            var elbow = origin + axis * along + perpendicular * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - origin, elbow - origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
        }
    }
}
```

### 6.2 카메라와 캐릭터 배치

1. Workshop 아래 빈 PlayerLook을 만들고 Position `(0,1.6,-0.45)`로 설정한다.
2. PlayerLook에 FirstPersonRig를 붙인다.
3. PlayerLook 밑에 Camera를 만들고 Local Position/Rotation을0으로 한다. 기존 기본 Main Camera가 있다면 제거해 중복을 막는다.
4. 카메라 Tag=MainCamera, FOV=68, Near=0.12, Far=30으로 설정한다. Background Type은 Solid Color, 색은 어두운 회청색으로 설정한다. URP Inspector에서는 배경 설정 항목의 표시 방식이 다를 수 있다.
5. 카메라에 AudioListener가 하나만 있는지 확인한다. Rig의 View 필드에 이 Camera를 넣는다.
6. Engineer.prefab을 PlayerLook 밑으로 끌어 넣고 Local Position `(0,-1.6,0)`, Rotation0, Scale1로 설정한다.
7. Engineer 아래 Animator 컴포넌트를 찾아 Enabled를 끈다. 컴포넌트를 제거할 필요는 없다.
8. SkinnedMeshRenderer의 Update When Offscreen을 모두 켠다.

### 6.3 본 연결과 소켓

Hierarchy 검색으로 본 이름을 찾아 아래 Rig 필드에 끌어 넣는다. `.r`과 `.l`을 바꾸지 않도록 확인한다.

| Rig 필드 | 대상 |
| --- | --- |
| Right Upper / Right Lower / Right Hand | upperarm.r / lowerarm.r / hand.r |
| Left Upper / Left Lower / Left Hand | upperarm.l / lowerarm.l / hand.l |
| Left Arm | Engineer_ArmLeft의 Renderer |

hand.r의 자식으로 빈 HammerGrip, hand.l의 자식으로 빈 ItemGrip을 만든다. 둘 다 Local Position/Rotation0, Scale1이다. Rig의 Hammer Grip과 Item Grip에 각각 연결한다. Engineer 원본 프리팹은 리그를 편집할 수 있는 형태다. 자식 추가가 제한된 다른 프리팹을 사용한다면 Workshop 안의 인스턴스에서 Prefab > Unpack을 한 뒤 작업하고, 원본 자산은 보존한다.

### 6.4 망치와 표시 레이어

1. UpgradeHammer.fbx를 HammerGrip 아래에 놓는다.
2. 망치 Local Position을 `(0,0.18,0.03)`, Rotation0으로 한다. 모델 최장 길이가 약0.4m가 되도록 균등 Scale을 조정한다. 도구를 사용하는 경우13장의 정규화 코드가 이를 계산한다.
3. Edit > Project Settings > Tags and Layers에서 레이어30을 FirstPersonArms, 31을 PlayerBody로 이름 붙인다.
4. Engineer_ArmRight와 Engineer_ArmLeft의 **Renderer가 있는 오브젝트**는30, 나머지 캐릭터 Renderer 오브젝트는31로 한다. 본마다 일괄 레이어를 바꾸지 않는다.
5. 망치의 모든 모델 자식은30으로 한다. 레이어 적용 창에서 망치 자식 전체에 적용한다.
6. 카메라 Culling Mask에서 PlayerBody만 제외한다. Scene뷰의 Layers에서는 두 레이어를 모두 표시한다.
7. 팔과 망치 Renderer의 Shadow Casting Mode를 Off로 하고, 캐릭터와 망치의 Collider는 제거한다. 상품 Collider는 나중에 코드로 생성하므로 여기서 건드리지 않는다.

**완료 확인:** Play를 누르고 Game뷰를 클릭한 뒤 우클릭 드래그하면 좌우 거치대가 보인다. 머리와 몸은 안 보이고 오른손 망치가 보인다. 왼손이 비어 있으므로 왼팔은 게임 화면에서 숨겨진다. Scene뷰에서는 전신 모델을 확인할 수 있다. 이 단계는 아직 Bootstrap이 없어 실습 씬의 Workshop이 직접 실행된다.

**문제 해결:** 팔이 처져 있으면 본 참조와 Animator 비활성화를 확인한다. 망치가 화면을 가리면 먼저 모델 크기와 손잡이 위치를 조정한다. 카메라 FOV를 과하게 늘려 해결하지 않는다.

## 7장 조명과 화면 정보

### 7.1 내부 조명

Workshop 자식으로 다음 Light들을 만든다. 카메라와 같은 PlayerLook 자식으로 만들지 않는다.

| 이름 | Type | Position | 색 RGB | Intensity | Range |
| --- | --- | --- | --- | ---: | ---: |
| WorkLight | Spot | `(0,2.85,1.2)` | `(1,0.73,0.42)` | 9 | 6 |
| SupplyLight | Point | `(-1.9,2.3,0.3)` | `(1,0.62,0.28)` | 3 | 3.2 |
| StorageLight | Point | `(1.9,2.3,0.3)` | `(0.25,0.7,1)` | 3 | 3.2 |
| FillLight | Point | `(0,2.2,-0.7)` | `(0.5,0.6,0.7)` | 0.8 | 4 |
| ResultLight | Point | `(0,1.8,1.4)` | White | 0 | 2 |

WorkLight의 Rotation은 `(90,0,0)`, Spot Angle은76이다. 기본 Directional Light가 있다면 제거한다. URP Asset에서 Additional Lights가 켜져 있는지 확인한다. 작은 조명용 Cube와 케이블을 추가할 수 있지만 빛을 내는 위치 바로 앞을 불투명 모델로 막지 않는다.

Window > Rendering > Lighting의 Environment에서 Skybox Material을 비우고 Environment Lighting의 Source를 Color로 설정한다. 환경색은 `(0.045,0.055,0.065)`를 기준으로 한다. Fog를 사용하면 Exponential Squared, 색 `(0.015,0.02,0.025)`, Density0.055로 한다. 최종 Bootstrap 코드도 같은 환경 설정을 적용한다.

### 7.2 Canvas와 Text

1. Hierarchy에서 UI > Canvas를 만들고 Workshop 아래로 옮긴다. 이름은 WorkshopHUD다.
2. Canvas Render Mode=Screen Space Overlay로 한다.
3. CanvasScaler의 UI Scale Mode=Scale With Screen Size, Reference Resolution=1600×900, Match=0.5로 한다.
4. Canvas 아래에 Legacy Text4개를 만든다. 버전에 따라 UI > Legacy > Text 메뉴에서 만들 수 있다. 코드의 필드는 UnityEngine.UI.Text이므로 TextMeshProUGUI를 대신 연결하지 않는다.
5. 아래 표대로 RectTransform의 Anchor Min, Anchor Max, Pivot을 모두 같은 Anchor 값으로 설정한 뒤 위치와 크기를 입력한다.

| 이름 | Anchor/Pivot | Pos X/Y | Width/Height | Font Size | Alignment |
| --- | --- | --- | --- | ---: | --- |
| StatusText | `(0,1)` | `(32,-25)` | `(1100,90)` | 25 | Upper Left |
| ItemText | `(0,0)` | `(32,105)` | `(1450,85)` | 24 | Lower Left |
| HelpText | `(0,0)` | `(32,24)` | `(1450,65)` | 19 | Lower Left |
| MessageText | `(0.5,1)` | `(0,-130)` | `(1450,60)` | 23 | Middle Center |

Text 색은 옅은 크림색으로 하고 Raycast Target은 끈다. Outline을 추가해 검은색, Effect Distance `(1,-1)`로 설정한다. 기본 텍스트를 임의로 입력해 위치를 확인한다. 영어 UI는 기본 LegacyRuntime 폰트로 표시할 수 있다. 한국어 UI로 바꾸려면 한글 글리프가 있는 폰트를 사용한다.

자동 생성된 EventSystem은 이 실습에서는 사용하지 않는다. 입력은3D Raycast이고 UI Button이 없으므로 제거해도 된다.

### 7.3 공간 표지

공급·보관대와 TAKE, SELL, NEXT ROUND는 TextMesh로 표지를 붙이면 구별하기 쉽다. Workshop 또는 해당 Rack 아래 빈 오브젝트를 만들고 TextMesh 컴포넌트를 추가한다. Anchor=Middle Center, Alignment=Center로 두고 텍스트를 입력한다. Cube 뒤에 가리지 않도록 표면 앞에 약간 띄운다. 정확한 위치와 기본 크기는13장의 Editor 생성 코드에도 포함되어 있다.

**완료 확인:** Game뷰에서 어두운 방 안의 상품 영역과 작업대가 구분되고 임시 UI 텍스트가 보인다. ResultLight는 강도0이라 평상시 빛나지 않는다.

## 8장 게임 규칙 코드 작성

### WorkshopGame.cs

Runtime에 아래 전체 파일을 작성한다. 타입과 상태가 서로 연결되어 있어 이 단계에서는 파일 전체를 넣고 컴파일한 뒤 기능별로 확인한다. 일부 메서드만 먼저 붙이면 아직 없는 메서드 호출 때문에 오류가 생길 수 있다.

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ReinforceGame
{
    public sealed class WorkshopGame : MonoBehaviour
    {
        public FirstPersonRig rig;
        public Transform benchPivot;
        public Transform[] supplyPivots, storagePivots;
        public Text statusText, itemText, helpText, messageText;
        public Light resultLight;
        ReinforceSettings settings;
        WorkshopItem held, bench;
        WorkshopItem hovered;
        readonly List<WorkshopItem> inventory = new List<WorkshopItem>();
        InputAction click, reset;
        int round = 1, revenue, strikes;
        bool busy, initialized;
        string message = "LEFT: supply rack  /  CENTER: workbench  /  RIGHT: storage";
        float messageUntil;
        const string SaveKey = "ReinforceGame.Save.v1";

        [Serializable] sealed class SavedItem { public string id; public int level; public string location; public int slot; }
        [Serializable] sealed class SaveState { public int round = 1; public int revenue; public int strikes; public List<SavedItem> items = new List<SavedItem>(); }

        public void Initialize(ReinforceSettings data)
        {
            settings = data;
            click = new InputAction("Interact", InputActionType.Button, "<Mouse>/leftButton");
            reset = new InputAction("Retry", InputActionType.Button, "<Keyboard>/r");
            click.Enable(); reset.Enable();
            initialized = true;
            if (!Load()) Refill();
            RefreshUI();
        }
        void OnDestroy() { click?.Dispose(); reset?.Dispose(); }
        void Update()
        {
            if (!initialized) return;
            rig.carrying = held != null;
            hovered = null;
            if (Mouse.current != null)
            {
                var hoverHits = Physics.RaycastAll(rig.view.ScreenPointToRay(Mouse.current.position.ReadValue()), 8f, ~(1 << 30 | 1 << 31));
                Array.Sort(hoverHits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hoverHits)
                {
                    var candidate = hit.collider.GetComponentInParent<WorkshopItem>();
                    if (candidate && candidate == held) continue;
                    hovered = candidate; break;
                }
            }
            if (!busy && reset.WasPressedThisFrame() && IsBankrupt())
            {
                revenue = 0; strikes = 0; Refill(); Save(); Say("Round retried. The target is unchanged.");
            }
            if (!busy && click.WasPressedThisFrame() && Mouse.current != null && !Mouse.current.rightButton.isPressed)
                Interact();
            RefreshUI();
        }
        void Interact()
        {
            Ray ray = rig.view.ScreenPointToRay(Mouse.current.position.ReadValue());
            var hits = Physics.RaycastAll(ray, 8f, ~(1 << 30 | 1 << 31));
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            RaycastHit hit = default;
            bool found = false;
            foreach (var candidate in hits)
            {
                if (candidate.collider.GetComponentInParent<WorkshopItem>() == held && held) continue;
                hit = candidate; found = true; break;
            }
            if (!found) return;
            var item = hit.collider.GetComponentInParent<WorkshopItem>();
            if (item)
            {
                if (held) { Say("Place the held item first."); return; }
                if (item == bench) { StartCoroutine(Reinforce()); return; }
                Pickup(item); return;
            }
            var station = hit.collider.GetComponentInParent<WorkshopStation>();
            if (!station) return;
            UseStation(station);
        }
        void UseStation(WorkshopStation station)
        {
            switch (station.kind)
            {
                case StationKind.Bench:
                    if (station.index == 1)
                    {
                        if (bench && !held) Pickup(bench);
                    }
                    else if (held)
                    {
                        if (bench) { Say("The workbench is occupied."); break; }
                        bench = held; held = null; bench.storageSlot = -1;
                        bench.Dock(benchPivot, bench.definition.benchEuler);
                        Save(); Say("Click REINFORCE to strike. TAKE retrieves the item.");
                    }
                    else if (bench) StartCoroutine(Reinforce());
                    break;
                case StationKind.Storage:
                    if (!held) { Say("Carry an item here to keep it for future rounds."); break; }
                    if (inventory.Exists(x => x != held && x.storageSlot == station.index)) { Say("This storage slot is occupied."); break; }
                    held.storageSlot = station.index;
                    held.Dock(storagePivots[station.index], held.definition.displayEuler);
                    held = null; Save(); Say("Stored. This item will survive the round change."); break;
                case StationKind.Sell:
                    if (!held) { Say("Carry an item to the selling tray."); break; }
                    int price = settings.Price(held.definition, held.level);
                    revenue += price; Remove(held); held = null; Save(); Say($"Sold for {price:N0}. Round sales: {revenue:N0}."); break;
                case StationKind.NextRound:
                    if (revenue < settings.Target(round)) { Say("Reach the sales target before advancing."); break; }
                    if (held || bench) { Say("Sell or store the active item before advancing."); break; }
                    round++; revenue = 0; strikes = 0; Refill(); Save(); Say("New target. Supply refilled; stored items retained."); break;
            }
        }
        void Pickup(WorkshopItem item)
        {
            if (item == bench) bench = null;
            held = item; item.storageSlot = -1;
            item.Dock(rig.itemGrip, item.definition.gripEuler, GripOffset(item));
            Save(); Say("Carry to the workbench, storage rack, or selling tray.");
        }
        Vector3 GripOffset(WorkshopItem item) => item.definition.gripOffset - Quaternion.Euler(item.definition.gripEuler) * item.visual.localPosition;
        IEnumerator Reinforce()
        {
            if (bench.level >= settings.MaxLevel) { Say("Maximum upgrade reached. Sell or store this item."); yield break; }
            busy = true;
            float time = 0;
            while (time < .5f)
            {
                time += Time.deltaTime; rig.strike = Mathf.Sin(Mathf.Clamp01(time / .5f) * Mathf.PI);
                yield return null;
            }
            rig.strike = 0; strikes++;
            float roll = UnityEngine.Random.value;
            float success = settings.Chance(bench.level), destruction = settings.Destruction(bench.level);
            if (roll < success)
            {
                bench.level++; resultLight.color = new Color(.25f, 1, .65f);
                Say($"SUCCESS  +{bench.level}  |  Value {settings.Price(bench.definition, bench.level):N0}");
            }
            else if (roll < success + destruction)
            {
                Remove(bench); bench = null; resultLight.color = new Color(1, .15f, .07f); Say("DESTROYED. Choose another item or retrieve one from storage.");
            }
            else
            {
                bench.level = Mathf.Max(0, bench.level - 1); resultLight.color = new Color(1, .6f, .1f);
                Say($"DOWNGRADE  +{bench.level}  |  Level cannot fall below zero.");
            }
            Save(); resultLight.intensity = 4;
            yield return new WaitForSeconds(.35f);
            resultLight.intensity = 0; busy = false;
        }
        void Remove(WorkshopItem item) { inventory.Remove(item); Destroy(item.gameObject); }
        WorkshopItem Spawn(ItemDefinition definition, Transform pivot, Vector3 rotation)
        {
            var root = new GameObject(definition.title);
            var item = root.AddComponent<WorkshopItem>(); item.definition = definition;
            var visual = Instantiate(definition.prefab, root.transform); item.visual = visual.transform;
            foreach (var collider in visual.GetComponentsInChildren<Collider>()) Destroy(collider);
            var renderers = visual.GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(); bool first = true;
            foreach (var renderer in renderers) { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
            float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            float scale = longest > .0001f ? definition.size / longest : 1;
            visual.transform.localScale *= scale;
            visual.transform.localPosition -= bounds.center * scale;
            var box = root.AddComponent<BoxCollider>(); box.size = Vector3.Max(bounds.size * scale, Vector3.one * .12f);
            item.Dock(pivot, rotation); inventory.Add(item); return item;
        }
        void Refill()
        {
            foreach (var item in inventory.ToArray()) if (item.storageSlot < 0) Remove(item);
            held = bench = null;
            for (int i = 0; i < supplyPivots.Length; i++) Spawn(settings.items[i % settings.items.Length], supplyPivots[i], settings.items[i % settings.items.Length].displayEuler);
        }
        bool IsBankrupt() => inventory.Count == 0 && revenue < settings.Target(round);
        void Say(string value) { message = value; messageUntil = Time.unscaledTime + 5; }
        void RefreshUI()
        {
            statusText.text = $"THE LAST FORGE     /     ROUND {round:00}\nSALES  {revenue:N0} / {settings.Target(round):N0}     STRIKES  {strikes}  /  UNLIMITED";
            var item = hovered ? hovered : bench ? bench : held;
            if (item)
            {
                int level = item.level;
                string chance = level >= settings.MaxLevel ? "MAXIMUM LEVEL" : $"UP {settings.Chance(level):P0}    DOWN {1 - settings.Chance(level) - settings.Destruction(level):P0}    BREAK {settings.Destruction(level):P0}";
                string location = item == bench ? "WORKBENCH" : item == held ? "IN HAND" : item.storageSlot >= 0 ? "STORAGE" : "SUPPLY";
                itemText.text = $"{location}   {item.definition.title} +{level}\nVALUE {settings.Price(item.definition, level):N0}     {chance}";
            }
            else itemText.text = "WORKBENCH EMPTY\nChoose an item from the supply rack on your left.";
            helpText.text = "RMB DRAG  Look around     LMB  Select / place / use\nLEFT  Supply     FRONT  Reinforce & sell     RIGHT  Storage";
            if (IsBankrupt()) messageText.text = "NO ITEMS REMAIN / TARGET MISSED     Press R to retry this round.";
            else if (Time.unscaledTime < messageUntil) messageText.text = message;
            else if (revenue >= settings.Target(round)) messageText.text = "TARGET REACHED  /  Store valuable items, then click NEXT ROUND.";
            else messageText.text = "No upgrade cost. Every strike risks downgrade or destruction.";
        }
        void Save()
        {
            var state = new SaveState { round = round, revenue = revenue, strikes = strikes };
            foreach (var item in inventory)
            {
                string location = item == held ? "hand" : item == bench ? "bench" : item.storageSlot >= 0 ? "storage" : "supply";
                int slot = item.storageSlot;
                if (location == "supply") slot = Array.IndexOf(supplyPivots, item.transform.parent);
                state.items.Add(new SavedItem { id = item.definition.id, level = item.level, location = location, slot = slot });
            }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(state)); PlayerPrefs.Save();
        }
        bool Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            SaveState state;
            try { state = JsonUtility.FromJson<SaveState>(PlayerPrefs.GetString(SaveKey)); }
            catch (Exception) { return false; }
            if (state == null || state.round < 1 || state.items == null) return false;
            foreach (var entry in state.items)
                if (Array.Find(settings.items, x => x.id == entry.id) == null ||
                    (entry.location == "storage" && (entry.slot < 0 || entry.slot >= storagePivots.Length)) ||
                    (entry.location == "supply" && (entry.slot < 0 || entry.slot >= supplyPivots.Length))) return false;
            round = state.round; revenue = state.revenue; strikes = state.strikes;
            foreach (var entry in state.items)
            {
                var definition = Array.Find(settings.items, x => x.id == entry.id);
                Transform pivot = entry.location == "hand" ? rig.itemGrip : entry.location == "bench" ? benchPivot : entry.location == "storage" ? storagePivots[entry.slot] : supplyPivots[entry.slot];
                var item = Spawn(definition, pivot, entry.location == "bench" ? definition.benchEuler : entry.location == "hand" ? definition.gripEuler : definition.displayEuler);
                item.level = Mathf.Clamp(entry.level, 0, settings.MaxLevel);
                if (entry.location == "hand") { held = item; item.transform.localPosition = GripOffset(item); }
                if (entry.location == "bench") bench = item;
                if (entry.location == "storage") item.storageSlot = entry.slot;
            }
            return true;
        }
    }
}
```

### 코드가 처리하는 순서

| 구간 | 읽을 메서드 | 역할 |
| --- | --- | --- |
| 시작 | Initialize | 설정 수신, 입력 활성화, 저장 복원 또는 공급 생성 |
| 매 프레임 | Update, Interact | hover와 클릭, 재도전, UI 갱신 |
| 위치 이동 | UseStation, Pickup, GripOffset | 손·작업대·보관·판매 이동 |
| 강화 | Reinforce | 1회 연출, 단일 난수 추첨, 결과 저장 |
| 상품 관리 | Spawn, Remove, Refill | 정규화, Collider, 생존 목록, 리필 |
| 표시 | RefreshUI, Say | 금액·확률·결과 안내 |
| 저장 | Save, Load | 상품id와 위치를 JSON으로 기록·복원 |

**핵심 확인:** inventory에는 살아 있는 상품만 있다. 상품은 held, bench, 보관 슬롯, 공급 피벗 중 하나에 속한다. 강화 비용과 남은 시도 횟수 필드는 없다. strikes는 제한이 아니라 통계다.

**완료 확인:** 컴파일 오류가 없다. 아직 WorkshopGame을 초기화하지 않았으므로 상품이 나타나지 않아도 정상이다. 이 코드의 Initialize는 다음 장에서 연결하고10장에서 Bootstrap이 호출한다.

## 9장 Workshop 참조 연결과 프리팹 저장

### Inspector 연결

1. Workshop 루트에 WorkshopGame을 추가한다.
2. 다음 표대로 Scene 오브젝트를 끌어 넣는다.

| 필드 | 연결 대상 |
| --- | --- |
| Rig | PlayerLook의 FirstPersonRig |
| Bench Pivot | BenchItemPivot |
| Supply Pivots | Size6, SupplyPivot0~5를 순서대로 |
| Storage Pivots | Size6, StoragePivot0~5를 순서대로 |
| Status Text | StatusText |
| Item Text | ItemText |
| Help Text | HelpText |
| Message Text | MessageText |
| Result Light | ResultLight |

배열의0번과1번을 같은 피벗으로 연결하지 않는다. 슬롯의 index와 피벗 배열 index가 맞아야 한다. FirstPersonRig의 View와 양손 소켓도 다시 확인한다.

### 프리팹과 설정 연결

1. Workshop 루트를 Project 창의 Prefabs 폴더에 끌어 넣어 Workshop.prefab을 만든다.
2. Resources/ReinforceSettings.asset을 선택한다.
3. Workshop 필드에 **Project 창의 Workshop.prefab 자산**을 넣는다. Scene 인스턴스를 연결하지 않는다.
4. Items의6개 상품 프리팹과 가격·확률을 확인한다.
5. File > Save로 실습 씬을 저장한다.

**완료 확인:** Workshop.prefab을 Prefab Mode로 열어도 카메라·본·UI·피벗 참조가 None이 아니다. 설정 자산의 Workshop이 해당 프리팹을 가리킨다.

## 10장 Bootstrap 씬으로 실행

### 10.1 Bootstrap.cs

Runtime 폴더에 아래 전체 파일을 만든다.

```csharp
using UnityEngine;

namespace ReinforceGame
{
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] ReinforceSettings settings;
        [SerializeField] GameObject editorPreview;
        void Awake()
        {
            if (editorPreview) { editorPreview.SetActive(false); Destroy(editorPreview); }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.045f, .055f, .065f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.015f, .02f, .025f);
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .055f;
            if (!settings) settings = Resources.Load<ReinforceSettings>("ReinforceSettings");
            if (!settings || !settings.workshop)
            {
                Debug.LogError("ReinforceGame: Generate Workshop in the Editor first.");
                enabled = false;
                return;
            }
            var room = Instantiate(settings.workshop);
            room.name = "Reinforce Workshop";
            room.GetComponent<WorkshopGame>().Initialize(settings);
        }
    }
}
```

### 10.2 시작 씬 구성

1. WorkshopPractice 씬을 저장한 뒤 File > New Scene으로 빈 씬을 만든다.
2. `Assets/ReinforceGame/Scenes/Bootstrap.unity`로 저장한다.
3. 기본 Camera나 Directional Light가 있다면 제거한다.
4. 빈 GameObject를 만들고 이름을 Bootstrap으로 한다. Bootstrap 컴포넌트를 붙인다.
5. Settings에 Resources/ReinforceSettings.asset을 넣는다.
6. Workshop.prefab을 씬에 하나 끌어 넣고 이름을 EditorPreview로 바꾼다.
7. Bootstrap의 Editor Preview에 이 Scene 인스턴스를 넣는다.
8. 씬을 저장하고 Play한다.

실행 직후 EditorPreview는 비활성화·제거되고 설정의 Workshop 프리팹이 Reinforce Workshop이라는 이름으로 생성된다. 실제 게임은 이 새 인스턴스다. Scene뷰에 전신을 보이게 하려고 프리뷰를 남기더라도 실행 중에는 반드시 교체해야 한다.

**완료 확인:** 새 저장이면1라운드, 판매액0/목표250, 공급6개가 보인다. Hierarchy에 실행용 작업실은 하나이며 활성 Camera와 AudioListener도 하나다. 예전 진행이 복원되면12장의 저장 초기화를 먼저 수행한다.

## 11장 실제 플레이로 기능 확인

### 11.1 집기와 작업대

1. 우클릭 드래그로 왼쪽 공급대를 본 뒤 버튼을 놓는다.
2. 한손검을 좌클릭한다. 검이 왼손으로 이동하고 왼팔이 표시되어야 한다.
3. 정면으로 돌아와 빈 작업대 천판 또는 강화판을 클릭한다.
4. 검이 BenchItemPivot에 놓이고 왼손이 숨겨지는지 확인한다.
5. TAKE를 클릭해 다시 들고, 작업대에 다시 놓는다.

이동할 때 상품이 복제되거나 이전 위치에 남아 있으면 inventory와held/bench, Dock의 부모 변경을 확인한다. 상품을 든 뒤 클릭이 막히면 Interact에서 자신의 상품 Collider를 제외하는지 확인한다.

### 11.2 강화

작업대 상품 또는 강화판을 클릭한다. 오른손 망치 연출 뒤 단계·가치·시도 횟수가 갱신되는지 확인한다. 연타해도 연출 중에는 다음 시도가 쌓이지 않아야 한다. 테스트 중 확률 결과는 무작위이므로 파괴가 나왔다고 코드 오류로 판단하지 않는다.

### 11.3 판매와 보관

1. TAKE로 상품을 들고 SELL을 클릭한다. 표시 가격만큼 판매액이 오르고 상품이 사라져야 한다.
2. 다른 상품을 집어 오른쪽 빈 보관 칸의 클릭 면을 누른다.
3. 상품이 해당 피벗에 고정되면 빈손으로 상품을 다시 클릭해 회수한다.
4. 상품을 다시 보관한 뒤 그 상품에 포인터를 올려 단계와 판매가를 확인한다.

보관 상품을 직접 클릭하면 집는다. 이미 상품을 든 상태에서 다른 상품을 클릭하면 교환하지 않는다. 보관 칸이 찼다면 다른 빈 칸을 사용하거나 먼저 기존 상품을 꺼내 판매한다.

### 11.4 다음 라운드

현재 판매액이 목표 미만인 상태에서 NEXT ROUND를 눌러 진행이 막히는지 확인한다. 상품을 강화·판매해 목표를 채우고, 남겨 둘 상품은 보관한다. 손과 작업대가 빈 상태로 NEXT ROUND를 클릭한다.

**완료 확인:** 라운드2, 판매액0/목표362, 시도 횟수0이며 공급대에는+0상품6개가 있다. 보관 상품의 종류와 단계는 그대로다. 목표를 넘긴 판매액은 이월되지 않는다.

## 12장 확률 분기와 저장을 확실히 검사

### 12.1 테스트용 설정 복사

1. Play를 멈춘다. 본래 설정 자산을 복제해 Resources 밖에 ReinforceSettings_Test.asset으로 저장한다. 이름이 다른 파일도 Resources 안에 있으면 로드 경로와 혼동하기 쉬우므로 테스트용 폴더를 사용한다.
2. Bootstrap.Settings에 테스트 복사본을 연결한다. 프리뷰가 아닌 Bootstrap 컴포넌트를 선택했는지 확인한다.
3. 확률 배열8개 값을 아래 표대로 바꿔 한 분기씩 테스트한다.

| 시험 | Success Rates 전체 | Destruction Rates 전체 | 결과 |
| --- | --- | --- | --- |
| 성공 | 1 | 0 | 단계1 증가 |
| 하락 | 0 | 0 | 단계1 감소, +0에서는 유지 |
| 파괴 | 0 | 1 | 상품 제거 |

하락 테스트 전에 성공 설정으로+1이상 상품을 만들고 저장한 뒤 Play를 멈춘다. 하락 설정으로 다시 실행하면 저장 상품을 이어서 검사할 수 있다. +8까지 성공시킨 뒤 다시 강화해 최대 단계에서 시도 수가 증가하지 않는지도 확인한다.

ScriptableObject 자산의 Play 중 변경은 유지될 수 있다. 정지하면 무조건 복구된다고 가정하지 않는다. 검사가 끝나면 Bootstrap.Settings를 원래 자산으로 돌린다.

### 12.2 저장 복원

서로 다른 상품을 손, 작업대, 보관대에 두는 상태를 나누어 검사한다. 손과 작업대에는 서로 다른 상품이 동시에 존재할 수 있다. 각 이동 후 자동 저장되므로 Stop→Play하여 다음을 확인한다.

- round, revenue, strikes가 유지된다.
- 각 상품의 종류와 단계가 유지된다.
- 손과 작업대의 부모가 올바르게 복원된다.
- 보관 슬롯index가 유지된다.
- 이미 팔거나 파괴된 상품이 다시 살아나지 않는다.

저장키는 ReinforceGame.Save.v1이다. Play 중 강화 결과가 확정되기 전에 정지하면 마지막 확정 상태에서 복원되는 것이 정상이다.

### 12.3 실패와 저장 초기화

파괴100% 테스트 설정으로 남은 공급·보관 상품을 모두 파괴한다. 목표액이 미달이면 실패 안내가 나와야 한다. R을 누르면 같은 라운드, 판매액0과 공급6개로 재도전한다.

진행 초기화는13장의 Reset Saved Progress 메뉴를 사용한다. 아직 Editor 도구를 작성하지 않았다면 다음 파일을 `Editor/ProgressResetMenu.cs`에 임시로 작성한다.

```csharp
using UnityEditor;
using UnityEngine;

namespace ReinforceGame.Editor
{
    public static class ProgressResetMenu
    {
        [MenuItem("Reinforce Game/Reset Tutorial Progress")]
        public static void Reset()
        {
            PlayerPrefs.DeleteKey("ReinforceGame.Save.v1");
            PlayerPrefs.Save();
            Debug.Log("Progress cleared. Restart Play mode.");
        }
    }
}
```

Play를 멈춘 상태에서 메뉴를 실행한 뒤 다시 Play한다. 다른 프로젝트 설정까지 지우는 PlayerPrefs.DeleteAll은 사용하지 않는다.13장의 정식 메뉴를 추가한 뒤 이 임시 파일은 제거해도 된다.

## 13장 Editor 도구로 재생성하기

### 수동 제작과 생성 도구의 관계

앞 단계에서는 오브젝트와 연결을 직접 만들며 구조를 확인했다. 이제 같은 결과를 코드로 재현한다. 이 도구는 작업실 프리팹과 Bootstrap 씬, 재질과상품 설정을 생성한다. 실행 시 방을 처음부터 조립하는 코드는 Bootstrap에 두지 않는다.

생성 도구는 **Workshop.prefab과Bootstrap.unity를 덮어쓴다**. 지금 만든 수동 결과를 보존하려면 파일을 복제해 Manual_Workshop.prefab, Manual_Bootstrap.unity로 저장한다. WorkshopPractice 씬도 남겨 둔다. 상품별 직접 조정값을 유지하려면 아래 Generator의 해당 값도 함께 수정한다.

### WorkshopGenerator.cs

Editor 폴더에 아래 전체 파일을 작성한다. Runtime 폴더에는 넣지 않는다. 기존 프로젝트에 이미 파일이 있다면 중복 생성하지 않는다.

```csharp
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ReinforceGame.Editor
{
    // All scene, prefab, material and settings creation happens in the Editor.
    [InitializeOnLoad]
    public static class WorkshopGenerator
    {
        const string Root = "Assets/ReinforceGame";
        const string Pack = "Assets/KayKit/Characters/KayKit - Adventurers (for Unity)";
        const string SettingsPath = Root + "/Resources/ReinforceSettings.asset";
        const string WorkshopPath = Root + "/Prefabs/Workshop.prefab";
        const string ScenePath = Root + "/Scenes/Bootstrap.unity";
        static readonly string[] Products = { "sword_1handed", "axe_1handed", "shield_round", "staff", "dagger", "engineer_Wrench" };
        static Material wall, wood, metal, brass, teal;

        static WorkshopGenerator()
        {
            // Interactive projects use the explicit menu; do not mutate a user's unsaved scene on import.
        }
        static void GenerateIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += GenerateIfMissing; return; }
            if (!File.Exists(SettingsPath)) Generate();
        }

        [MenuItem("Reinforce Game/Generate Workshop")]
        public static void Generate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before generating.");
            Ensure(Root + "/Art"); Ensure(Root + "/Characters"); Ensure(Root + "/Products");
            Ensure(Root + "/Materials"); Ensure(Root + "/Resources"); Ensure(Root + "/Prefabs"); Ensure(Root + "/Scenes"); Ensure(Root + "/Validation");
            MoveSelection();
            var previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path))
                throw new InvalidOperationException("Save the current scene before generating, or open the generated Bootstrap scene.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode && string.IsNullOrEmpty(previous.path) ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            wall = Material("Charcoal concrete", new Color(.095f, .12f, .14f), .1f);
            wood = Material("Workbench oak", new Color(.22f, .105f, .045f), .05f);
            metal = Material("Black steel", new Color(.085f, .105f, .12f), .65f);
            brass = Material("Warm brass", new Color(.6f, .32f, .085f), .7f);
            teal = Material("Storage enamel", new Color(.04f, .24f, .25f), .35f);
            var settings = AssetDatabase.LoadAssetAtPath<ReinforceSettings>(SettingsPath);
            if (!settings) { settings = ScriptableObject.CreateInstance<ReinforceSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            settings.items = Products.Select((name, i) => new ItemDefinition
            {
                id = name, title = new[] { "Iron sword", "Forged axe", "Round shield", "Arcane staff", "Steel dagger", "Engineer wrench" }[i],
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Products/" + name + ".prefab"),
                basePrice = new[] { 35, 40, 32, 50, 25, 38 }[i], size = i == 3 ? .8f : .62f,
                displayEuler = new Vector3(0, 180, 0), gripEuler = new Vector3(0, 180, 0), benchEuler = new Vector3(90, 0, 0)
            }).ToArray();
            try
            {
                var room = new GameObject("Workshop");
                var game = room.AddComponent<WorkshopGame>();
                BuildRoom(room.transform);
                game.rig = BuildPlayer(room.transform);
                game.benchPivot = Pivot("Item mount - workbench", room.transform, new Vector3(0, 1.19f, 1.4f));
                var bench = Box("Workbench oak slab", room.transform, new Vector3(0, .94f, 1.5f), new Vector3(2.6f, .18f, 1.2f), wood);
                Station(bench, StationKind.Bench, game.benchPivot);
                for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Box("Bench steel leg", room.transform, new Vector3(x * 1.05f, .44f, 1.5f + z * .43f), new Vector3(.12f, .88f, .12f), metal);
                var plate = Box("REINFORCE", room.transform, new Vector3(0, 1.055f, 1.4f), new Vector3(1.1f, .05f, .65f), metal);
                Station(plate, StationKind.Bench, game.benchPivot);
                var take = Box("TAKE item", room.transform, new Vector3(-.95f, 1.09f, 1.04f), new Vector3(.42f, .12f, .28f), brass);
                Station(take, StationKind.Bench, game.benchPivot, 1);
                Label("TAKE", room.transform, new Vector3(-.95f, 1.18f, 1.02f), Quaternion.Euler(60, 0, 0), .045f, Color.white);
                var sell = Box("SELL tray", room.transform, new Vector3(1.0f, 1.07f, 1.45f), new Vector3(.48f, .08f, .55f), teal);
                Station(sell, StationKind.Sell, sell.transform);
                Label("SELL", room.transform, new Vector3(1f, 1.13f, 1.43f), Quaternion.Euler(60, 0, 0), .05f, new Color(.5f, 1, .85f));
                Label("REINFORCE", room.transform, new Vector3(0, 1.09f, 1.8f), Quaternion.Euler(65, 0, 0), .045f, new Color(1, .8f, .4f));
                game.supplyPivots = BuildRack(room.transform, false, 6);
                game.storagePivots = BuildRack(room.transform, true, 6);
                var next = Box("NEXT ROUND switch", room.transform, new Vector3(0, 1.65f, 2.82f), new Vector3(1.25f, .4f, .1f), teal);
                Station(next, StationKind.NextRound, next.transform);
                Label("NEXT ROUND", room.transform, new Vector3(0, 1.65f, 2.75f), Quaternion.identity, .065f, Color.white);
                Label("THE LAST FORGE", room.transform, new Vector3(0, 2.55f, 2.82f), Quaternion.identity, .11f, new Color(.8f, .65f, .4f));
                Label("MEET THE QUOTA. KEEP WHAT YOU DARE.", room.transform, new Vector3(0, 2.29f, 2.82f), Quaternion.identity, .032f, new Color(.6f, .65f, .65f));
                game.resultLight = Light("Strike flash", room.transform, new Vector3(0, 1.8f, 1.4f), Color.white, 0, 2, LightType.Point);
                BuildHUD(game, room.transform);
                settings.workshop = PrefabUtility.SaveAsPrefabAsset(room, WorkshopPath);
                UnityEngine.Object.DestroyImmediate(room);
                var boot = new GameObject("Bootstrap").AddComponent<Bootstrap>();
                var preview = (GameObject)PrefabUtility.InstantiatePrefab(settings.workshop);
                preview.name = "Editor Preview - replaced by Bootstrap at runtime";
                var serialized = new SerializedObject(boot);
                serialized.FindProperty("settings").objectReferenceValue = settings;
                serialized.FindProperty("editorPreview").objectReferenceValue = preview; serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
                File.WriteAllText(Root + "/Validation/generation.txt", "Generated with Unity Editor " + Application.unityVersion + "\n" + DateTime.UtcNow.ToString("O") + "\nScene: " + ScenePath + "\nProducts: " + settings.items.Length);
                Debug.Log("ReinforceGame generated. Open Assets/ReinforceGame/Scenes/Bootstrap.unity.");
            }
            finally
            {
                if (SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            Validate();
        }

        static void MoveSelection()
        {
            Move("Assets/KayKit/Art/Model/UpgradeHammer.fbx", Root + "/Art/UpgradeHammer.fbx");
            Move("Assets/KayKit/Art/Model/UpgradeHammer.png", Root + "/Art/UpgradeHammer.png");
            Move(Pack + "/Prefabs/Characters/Engineer.prefab", Root + "/Characters/Engineer.prefab");
            foreach (var name in Products) Move(Pack + "/Prefabs/Accessories/" + name + ".prefab", Root + "/Products/" + name + ".prefab");
            string[] selected = new[] { Root + "/Characters/Engineer.prefab", Root + "/Art/UpgradeHammer.fbx" }.Concat(Products.Select(x => Root + "/Products/" + x + ".prefab")).ToArray();
            foreach (string dependency in AssetDatabase.GetDependencies(selected, true))
            {
                if (!dependency.StartsWith("Assets/KayKit/", StringComparison.Ordinal) || AssetDatabase.IsValidFolder(dependency)) continue;
                string destination = Root + "/Art/Dependencies/" + dependency.Substring("Assets/KayKit/".Length);
                Ensure(Path.GetDirectoryName(destination).Replace('\\', '/'));
                Move(dependency, destination);
            }
        }
        static void Move(string from, string to)
        {
            if (!AssetDatabase.LoadMainAssetAtPath(from))
            {
                if (!AssetDatabase.LoadMainAssetAtPath(to)) throw new FileNotFoundException("Required asset missing: " + from);
                return;
            }
            string error = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
        }
        static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        static Material Material(string name, Color color, float metallic)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = color; material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", .3f); EditorUtility.SetDirty(material); return material;
        }
        static Transform Pivot(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform;
        }
        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        static void Station(GameObject go, StationKind kind, Transform pivot, int index = 0)
        {
            var station = go.AddComponent<WorkshopStation>(); station.kind = kind; station.pivot = pivot; station.index = index;
        }
        static void Label(string text, Transform parent, Vector3 position, Quaternion rotation, float size, Color color)
        {
            var go = Pivot(text, parent, position); go.localRotation = rotation;
            var label = go.gameObject.AddComponent<TextMesh>(); label.text = text; label.characterSize = size; label.fontSize = 48;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = color;
        }
        static Light Light(string name, Transform parent, Vector3 position, Color color, float intensity, float range, LightType type)
        {
            var light = Pivot(name, parent, position).gameObject.AddComponent<Light>(); light.type = type; light.color = color;
            light.intensity = intensity; light.range = range; light.shadows = LightShadows.Soft; return light;
        }
        static void BuildRoom(Transform root)
        {
            Box("Sealed floor", root, new Vector3(0, -.1f, 0), new Vector3(6, .2f, 6), wall);
            Box("Sealed ceiling", root, new Vector3(0, 3.3f, 0), new Vector3(6, .2f, 6), wall);
            for (int i = -1; i <= 1; i += 2)
            {
                Box("Solid wall - no exit", root, new Vector3(i * 3, 1.6f, 0), new Vector3(.2f, 3.2f, 6), wall);
                Box("Solid wall - no exit", root, new Vector3(0, 1.6f, i * 3), new Vector3(6, 3.2f, .2f), wall);
            }
            for (int i = -2; i <= 2; i++)
                Box("Floor seam", root, new Vector3(i, .004f, 0), new Vector3(.012f, .008f, 6), metal);
            var lamp = Light("Workbench pendant", root, new Vector3(0, 2.85f, 1.2f), new Color(1, .73f, .42f), 9, 6, LightType.Spot);
            lamp.transform.rotation = Quaternion.Euler(90, 0, 0); lamp.spotAngle = 76;
            Box("Pendant housing", root, new Vector3(0, 2.93f, 1.2f), new Vector3(.65f, .12f, .35f), brass);
            Box("Pendant cable", root, new Vector3(0, 3.1f, 1.2f), new Vector3(.025f, .35f, .025f), metal);
            Light("Supply warm pool", root, new Vector3(-1.9f, 2.3f, .3f), new Color(1, .62f, .28f), 3, 3.2f, LightType.Point);
            Light("Storage cool pool", root, new Vector3(1.9f, 2.3f, .3f), new Color(.25f, .7f, 1), 3, 3.2f, LightType.Point);
            Light("Soft visibility fill", root, new Vector3(0, 2.2f, -.7f), new Color(.5f, .6f, .7f), .8f, 4, LightType.Point);
        }
        static Transform[] BuildRack(Transform root, bool storage, int count)
        {
            float side = storage ? 1 : -1;
            var rack = Pivot(storage ? "STORAGE - persistent mounts" : "SUPPLY - refills each round", root, new Vector3(side * 2.25f, 0, .35f));
            rack.localRotation = Quaternion.Euler(0, side * 90, 0);
            Box("Rack backing", rack, new Vector3(0, 1.5f, .2f), new Vector3(2.4f, 2.25f, .14f), storage ? teal : wood);
            var pivots = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                var position = new Vector3((i % 3 - 1) * .73f, i < 3 ? 1.95f : 1.05f, 0);
                pivots[i] = Pivot("Fixed mount " + (i + 1), rack, position);
                var hook = Box("Mount " + (i + 1), rack, position + new Vector3(0, -.3f, .07f), new Vector3(.62f, .08f, .32f), metal);
                if (storage) Station(hook, StationKind.Storage, pivots[i], i);
                // Empty storage slots have a broad click target behind the mount.
                if (storage)
                {
                    var target = Box("Storage slot " + (i + 1), rack, position + new Vector3(0, 0, .09f), new Vector3(.67f, .73f, .04f), teal);
                    Station(target, StationKind.Storage, pivots[i], i);
                }
            }
            Label(storage ? "KEEP / NEXT ROUND" : "SUPPLY / FREE", rack, new Vector3(0, 2.76f, .06f), Quaternion.identity, .065f, storage ? new Color(.45f, .9f, 1) : new Color(1, .8f, .45f));
            return pivots;
        }
        static FirstPersonRig BuildPlayer(Transform root)
        {
            var look = Pivot("Player look pivot", root, new Vector3(0, 1.6f, -.45f));
            var rig = look.gameObject.AddComponent<FirstPersonRig>();
            var camera = Pivot("First person camera", look, Vector3.zero).gameObject.AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.nearClipPlane = .12f; camera.farClipPlane = 30; camera.fieldOfView = 68;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.015f, .02f, .025f);
            camera.cullingMask = ~(1 << 31); camera.gameObject.AddComponent<AudioListener>(); rig.view = camera;
            var character = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Characters/Engineer.prefab"));
            character.transform.SetParent(look, false); character.transform.localPosition = new Vector3(0, -1.6f, 0);
            foreach (var animator in character.GetComponentsInChildren<Animator>()) animator.enabled = false;
            foreach (var renderer in character.GetComponentsInChildren<Renderer>())
            {
                renderer.gameObject.layer = renderer.name.Contains("Arm") ? 30 : 31;
                if (renderer.name.Contains("Arm")) renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (renderer is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = true;
                if (renderer.name == "Engineer_ArmLeft") rig.leftArm = renderer;
            }
            Transform Find(string name) => character.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            rig.rightUpper = Find("upperarm.r"); rig.rightLower = Find("lowerarm.r"); rig.rightHand = Find("hand.r");
            rig.leftUpper = Find("upperarm.l"); rig.leftLower = Find("lowerarm.l"); rig.leftHand = Find("hand.l");
            if (!rig.rightHand || !rig.leftHand) throw new InvalidOperationException("Engineer hand bones were not found.");
            FirstPersonRig.Solve(rig.rightUpper, rig.rightLower, rig.rightHand, look.TransformPoint(new Vector3(.32f, -.32f, .58f)), Vector3.down + Vector3.right);
            FirstPersonRig.Solve(rig.leftUpper, rig.leftLower, rig.leftHand, look.TransformPoint(new Vector3(-.32f, -.35f, .65f)), Vector3.down - Vector3.right);
            rig.hammerGrip = Pivot("Hammer grip socket", rig.rightHand, Vector3.zero);
            rig.itemGrip = Pivot("Item grip socket", rig.leftHand, Vector3.zero);
            var hammer = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/UpgradeHammer.fbx"));
            hammer.transform.SetParent(rig.hammerGrip, false);
            var bounds = new Bounds(); bool first = true;
            foreach (var renderer in hammer.GetComponentsInChildren<Renderer>()) { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
            float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            hammer.transform.localScale *= .4f / Mathf.Max(longest, .001f);
            hammer.transform.localPosition = new Vector3(0, .18f, .03f);
            foreach (var renderer in hammer.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.Off;
            foreach (var t in hammer.GetComponentsInChildren<Transform>()) t.gameObject.layer = 30;
            foreach (var collider in character.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var collider in hammer.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            return rig;
        }
        static void BuildHUD(WorkshopGame game, Transform root)
        {
            var canvasGo = new GameObject("Workshop HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(root, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            Text Text(string name, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(canvasGo.transform, false);
                var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
                var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.alignment = alignment;
                text.color = new Color(.94f, .91f, .8f); text.raycastTarget = false;
                var outline = go.AddComponent<Outline>(); outline.effectColor = new Color(0, 0, 0, .9f); outline.effectDistance = new Vector2(1, -1); return text;
            }
            game.statusText = Text("Quota", new Vector2(0, 1), new Vector2(32, -25), new Vector2(1100, 90), 25, TextAnchor.UpperLeft);
            game.itemText = Text("Item and risk", new Vector2(0, 0), new Vector2(32, 105), new Vector2(1450, 85), 24, TextAnchor.LowerLeft);
            game.helpText = Text("Controls", new Vector2(0, 0), new Vector2(32, 24), new Vector2(1450, 65), 19, TextAnchor.LowerLeft);
            game.messageText = Text("Outcome", new Vector2(.5f, 1), new Vector2(0, -130), new Vector2(1450, 60), 23, TextAnchor.MiddleCenter);
        }

        [MenuItem("Reinforce Game/Open Bootstrap")]
        public static void OpenBootstrap() => EditorSceneManager.OpenScene(ScenePath);

        [MenuItem("Reinforce Game/Reset Saved Progress")]
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey("ReinforceGame.Save.v1"); PlayerPrefs.Save();
            Debug.Log("ReinforceGame saved progress cleared. Restart Play mode for a fresh game.");
        }

        [MenuItem("Reinforce Game/Validate Generated Assets")]
        public static void Validate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ReinforceSettings>(SettingsPath);
            if (!settings || !settings.workshop || settings.items.Any(x => !x.prefab)) throw new InvalidOperationException("Incomplete settings or item references.");
            if (settings.successRates.Length == 0 || settings.destructionRates.Length == 0 || settings.priceMultipliers.Length < 2) throw new InvalidOperationException("Missing probability or price data.");
            var game = settings.workshop.GetComponent<WorkshopGame>();
            if (!game || !game.rig || !game.rig.view || !game.rig.hammerGrip || !game.rig.itemGrip || !game.benchPivot || game.supplyPivots.Length != 6 || game.storagePivots.Length != 6)
                throw new InvalidOperationException("Incomplete workshop references.");
            if ((game.rig.view.cullingMask & (1 << 31)) != 0) throw new InvalidOperationException("Body is visible to first-person camera.");
            string[] outside = AssetDatabase.GetDependencies(WorkshopPath, true).Where(x => x.StartsWith("Assets/") && !x.StartsWith(Root + "/")).ToArray();
            if (outside.Length > 0) throw new InvalidOperationException("Dependencies outside ReinforceGame: " + string.Join(", ", outside));
            File.WriteAllText(Root + "/Validation/validation.txt", "PASS: prefab references, player rig, 6 supply mounts, 6 storage mounts, camera body culling, self-contained asset dependencies.\n" + DateTime.UtcNow.ToString("O"));
            Debug.Log("ReinforceGame validation passed.");
        }
    }
}
```

### 생성 실행

1. Console의 컴파일 오류가 없는지 확인한다.
2. Play를 멈추고 열려 있는 실습 씬을 저장한다. 이름 없는 새 씬에서 실행하지 않는다.
3. Reinforce Game > Generate Workshop을 실행한다.
4. Reinforce Game > Validate Generated Assets를 실행해 통과 로그를 확인한다.
5. Reinforce Game > Open Bootstrap으로 생성 씬을 연다.
6. Reset Saved Progress로 검증용 진행을 지우고 Play한다.
7. 11장의 집기·강화·판매·보관·라운드 전환을 다시 확인한다.

이 도구는 자동으로 Generate를 실행하지 않는다. 메뉴에서 명시적으로 호출한다. UnityEditor API가 런타임 빌드에 포함되지 않도록 폴더를 구분한다.

### 생성 코드에서 찾아볼 부분

| 메서드 | 바꿀 때의 용도 |
| --- | --- |
| MoveSelection | 선정 에셋과 의존 파일 이동 |
| BuildRoom | 방 크기, 작업등과 보조 조명 |
| BuildRack | 거치대 방향,6개 피벗과 클릭 면 |
| BuildPlayer | 본 연결, 카메라 마스크, 망치 크기와위치 |
| BuildHUD | Canvas와Text의 위치·크기 |
| Generate | 상품 정의, 작업대·스테이션 연결, 프리팹과시작 씬 저장 |
| Validate | 필수 참조와외부 Assets 의존 검사 |

생성 후 프리팹을 직접 수정해도 다음 생성 때 되돌아간다. 반복 유지할 변경은 생성 코드에 반영한다. 설정의 상품 정의도 재생성 시 초기화되므로, 별도 테스트 설정은 원본 설정과 구분한다.

## 14장 실행 파일 만들기와 최종 확인

1. Play를 멈추고 기본 설정 자산의 확률이 원래 값인지 확인한다.
2. Bootstrap.Settings가 테스트 복사본이 아닌 원래 ReinforceSettings인지 확인한다.
3. File > Build Profiles에서 Windows 빌드 프로필을 선택한다.
4. Scene List에 ReinforceGame/Scenes/Bootstrap.unity를 추가하고 첫 번째 활성 씬으로 둔다. WorkshopPractice와 SampleScene은 빌드 시작 씬으로 사용하지 않는다.
5. 빌드 출력 폴더는 Assets 밖의 전용 폴더를 선택한다.
6. Build And Run으로 실행하고 아래 목록을 검사한다.

| 최종 시험 | 통과 조건 |
| --- | --- |
| 첫 실행 | 상품 6개와 목표 금액 표시, 중복 카메라 없음 |
| 시점과 선택 | 우클릭 회전과 좌클릭 선택이 충돌하지 않음 |
| 몸 표시 | 전신은 편집에서만 보이고 게임에는 손과 망치 표시 |
| 상품 이동 | 공급→손→작업대→손→판매/보관 가능 |
| 강화 | 성공·하락·파괴, 최대 단계, 연타 제한이 일치 |
| 라운드 | 목표 미달 차단, 보관품 유지, 기본 상품 리필 |
| 실패 | 전 상품 소진 시 같은 라운드 재도전 |
| 재실행 | 이전 진행 복원 |
| 화면 크기 변경 | UI와 상품이 읽히고 클릭할 수 있음 |

Editor와 실행 파일은 실행 환경과 저장 위치가 다를 수 있으므로 각각의 새 진행과 복원을 확인한다. 에디터에서 지운 저장이 실행 파일에서도 반드시 지워졌다고 가정하지 않는다.

## 막혔을 때 확인 순서

오류가 생기면 현재 장의 완료 확인으로 돌아가 한 종류씩 해결한다.

1. **컴파일 오류:** Console의 첫 빨간 오류부터 해결한다. 클래스 중복, 파일명, using, Editor 폴더를 확인한다.
2. **NullReferenceException:** 오류 위치의 필드가 Inspector에서 None인지 확인한다. 프리팹 자산과 실행 인스턴스를 구별한다.
3. **입력이 없음:** Game 뷰 포커스, Active Input Handling, Action.Enable을 확인한다.
4. **클릭 불가:** Collider, 레이어, 거치대 앞뒤 방향, 손의 상품 제외 처리를 확인한다.
5. **팔 또는 모델 문제:** 본 연결, Animator, 레이어 30/31, 부모 Scale, 손잡이 보정을 확인한다.
6. **시작 상태가 다름:** 자동 저장이 복원되고 있는지 확인하고 필요하면 해당 게임 키만 초기화한다.

완성 후에는 기획서의 밸런스 표와 기술 설계서의 상태 계약을 기준으로 수치를 조정한다. 최대 단계와 공급 수를 고정한 채 목표액만 계속 올리면 장기 진행에 한계가 있으므로, 최종 라운드 또는 목표 증가 상한은 별도 게임 규칙으로 결정한다.
