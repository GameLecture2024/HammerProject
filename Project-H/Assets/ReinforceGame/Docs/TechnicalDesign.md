# 강화 작업실 기술 설계서

## 구현 범위와 읽는 순서

이 설계는 단일 작업실, 1인칭 손 리그, 마우스 상품 상호작용, 확률 강화, 판매, 라운드 보관, 로컬 저장을 구현한다. 게임 규칙과 밸런스의 기준은 [게임 기획서](GameDesign.md)다. 아래 순서대로 스크립트를 작성하고 Unity Inspector를 연결하면 각 단계를 독립적으로 확인할 수 있다.

핵심 구조는 **Editor에서 제작한 작업실 프리팹과 설정 데이터를 Bootstrap이 읽어 실행하는 방식**이다. 방, 작업대, 거치대, 카메라, 캐릭터와 UI는 프리팹 자산으로 저장한다. 실행 중에는 그 프리팹과 상품 인스턴스, 게임 상태를 생성한다.

에디터 조작과 전체 코드를 단계별로 따라가려면 [구현 실습 튜토리얼](ImplementationTutorial.md)을 사용한다. 이 설계서는 구조와 데이터 계약을 확인하는 기준으로 함께 읽는다.

## 프로젝트 설정

| 항목 | 기준 |
| --- | --- |
| Unity Editor | 6000.0.57f1 |
| 렌더 파이프라인 | URP 17.0.4 |
| 입력 패키지 | Input System 1.14.2 |
| UI | Unity UI 2.0.0의 Canvas와 `UnityEngine.UI.Text` |
| 플랫폼 | Windows PC, 키보드와 마우스 |
| 씬 | `Assets/ReinforceGame/Scenes/Bootstrap.unity` |

프로젝트는 URP 템플릿으로 준비한다. Package Manager에서 위 패키지 구성을 확인한다. Project Settings의 Player에서 Active Input Handling은 Input System Package 또는 Both로 설정한다. 현재 프로젝트는 Both이며 게임 코드는 새 Input System API만 사용한다.

URP Asset의 Additional Lights를 활성화하고 작업실의 여러 Point Light를 동시에 볼 수 있게 한다. 기본 Cube의 크기는 1 Unity unit = 1m로 해석한다. 작업실 루트와 빈 피벗의 Scale은 `(1,1,1)`로 유지하고, 크기 조절은 자식 모델에 적용한다.

### 폴더 구조

```text
Assets/ReinforceGame/
  Art/                     UpgradeHammer와 원본 의존 파일
    Dependencies/          모델, 재질, 텍스처, Avatar
  Characters/              Engineer.prefab
  Products/                상품 프리팹 6개
  Materials/               방과 가구의 URP 재질
  Runtime/                 실행용 스크립트
  Editor/                  자산 생성 도구와 Editor 전용 검증
  Resources/               ReinforceSettings.asset
  Prefabs/                 Workshop.prefab
  Scenes/                  Bootstrap.unity
  Docs/                    기획서와 기술 설계서
  Validation/              검증 결과와 카메라 이미지
```

`UnityEditor`를 참조하는 코드는 `Editor` 폴더에 둔다. 기존 `WorkshopSmokeProbe.cs`는 Runtime 폴더에 있지만 파일 전체가 `#if UNITY_EDITOR`로 감싸진 검증 코드라 플레이어 빌드에 포함되지 않는다.

### 모델 준비

| 역할 | 원래 KayKit 위치 | 사용할 위치 |
| --- | --- | --- |
| 망치 | `KayKit/Art/Model/UpgradeHammer.fbx` | `ReinforceGame/Art/UpgradeHammer.fbx` |
| 엔지니어 | `KayKit/Characters/KayKit - Adventurers (for Unity)/Prefabs/Characters/Engineer.prefab` | `ReinforceGame/Characters/Engineer.prefab` |
| 상품 | 동일 팩의 `Prefabs/Accessories/` | `ReinforceGame/Products/` |

상품 파일은 `sword_1handed`, `axe_1handed`, `shield_round`, `staff`, `dagger`, `engineer_Wrench`의 `.prefab`이다. 이미 이동된 프로젝트에서는 사용할 위치에서 찾는다.

Project 창에서 자산을 이동하면 Unity가 `.meta`의 GUID를 유지한다. 생성 도구에서는 `AssetDatabase.MoveAsset`을 사용한다. 프리팹의 FBX, 재질, 텍스처와 Avatar까지 `AssetDatabase.GetDependencies`로 확인해 `Art/Dependencies`로 이동한다. 연결된 FBX를 다시 내보내거나 `.meta`를 새로 생성하면 기존 프리팹 참조가 끊어질 수 있다.

## 스크립트 책임과 작성 순서

모든 클래스의 namespace는 `ReinforceGame`으로 통일한다. 파일명과 MonoBehaviour 클래스명은 일치시킨다.

| 순서 | 파일 | 책임과 필요한 연결 |
| --- | --- | --- |
| 1 | `ReinforceSettings.cs` | ScriptableObject 설정과 직렬화 가능한 `ItemDefinition` |
| 2 | `WorkshopItem.cs` | 상품 종류, 단계, 보관 슬롯, 모델, 부모 피벗 변경 |
| 3 | `WorkshopStation.cs` | `StationKind` 열거형, 종류, 슬롯/기능 index, pivot |
| 4 | `FirstPersonRig.cs` | 시점 입력, 카메라, 양팔 본, 망치/상품 소켓, 팔 자세 |
| 5 | `WorkshopGame.cs` | 입력 판정, 상품 목록, 강화, 판매, 라운드, UI, 저장 |
| 6 | `Bootstrap.cs` | 설정 로드, 환경 설정, 작업실 생성과 초기화 |
| 7 | `Editor/WorkshopGenerator.cs` | 모델 연결, 재질·공간·프리팹·시작 씬 생성과 검증 |

`WorkshopGame`은 처음에는 빈 `Initialize(ReinforceSettings data)` 메서드만 작성해 다른 클래스와의 컴파일을 맞춘 뒤, 아래 단계대로 기능을 채울 수 있다. 각 기능을 추가할 때 먼저 컴파일 오류를 해결하고 다음 단계로 넘어간다.

```text
Bootstrap 씬
  └─ Bootstrap.Awake
       ├─ 설정 참조 또는 Resources 로드
       ├─ Instantiate(settings.workshop)
       └─ WorkshopGame.Initialize(settings)
            ├─ 입력 활성화
            ├─ 저장 복원 또는 공급 생성
            └─ UI 갱신

WorkshopGame
  ├─ FirstPersonRig       시점과 손
  ├─ WorkshopItem[]       살아 있는 상품 목록
  ├─ WorkshopStation     클릭 대상 기능
  └─ ReinforceSettings   가격과 확률
```

## 설정 데이터 계약

### ReinforceSettings

`[CreateAssetMenu(menuName = "Reinforce Game/Settings")]`를 적용한 ScriptableObject다. Unity의 Assets > Create > Reinforce Game > Settings에서 생성할 수 있다.

| 필드 | 형식 | 초기값과 의미 |
| --- | --- | --- |
| `workshop` | GameObject | Workshop 프리팹 |
| `items` | ItemDefinition[] | 기획서 순서의 6개 상품 |
| `firstTarget` | int | 250 |
| `targetGrowth` | float | 1.45 |
| `successRates` | float[] | 0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2 |
| `destructionRates` | float[] | 0.05, 0.08, 0.12, 0.18, 0.25, 0.32, 0.4, 0.5 |
| `priceMultipliers` | float[] | 1, 1.8, 3.2, 5.5, 9, 15, 25, 42, 70 |

확률 배열의 index는 시도 전 단계, 가격 배열의 index는 현재 단계다. +8은 가격만 있고 다음 강화 확률은 없다. 계산 메서드는 다음 계약을 따른다.

```text
Target(round) = RoundToInt(firstTarget * Pow(targetGrowth, round - 1))
Chance(level) = successRates[유효 범위로 Clamp한 level]
Destruction(level) = Min(1 - Chance(level), destructionRates[Clamp한 level])
Price(item, level) = RoundToInt(item.basePrice * priceMultipliers[Clamp한 level])
MaxLevel = Min(successRates.Length, priceMultipliers.Length - 1)
```

위 코드는 계산식 설명이다. 실제 메서드에서는 `Mathf`와 올바른 반환 형식을 사용한다. 확률은 0~1, 성공률과 파괴율의 합은 1 이하로 설정한다. 확률 배열은 비어 있지 않아야 하고 가격 배열에는 최소 두 단계가 있어야 한다.

### ItemDefinition

`[Serializable]`을 붙인 일반 클래스로 작성해 설정 자산에 포함한다.

| 필드 | 형식 | 의미 |
| --- | --- | --- |
| id | string | 저장에 사용하는 고유 ID |
| title | string | 표시 이름 |
| prefab | GameObject | 상품 모델 프리팹 |
| basePrice | int | 기본 판매가 |
| size | float | 정규화한 모델의 최대 길이 |
| displayEuler | Vector3 | 공급·보관 시 로컬 회전 |
| gripEuler | Vector3 | 왼손에 들었을 때 로컬 회전 |
| gripOffset | Vector3 | 손잡이 위치의 추가 보정 |
| benchEuler | Vector3 | 작업대 위 로컬 회전 |

초기 displayEuler와 gripEuler는 `(0,180,0)`, benchEuler는 `(90,0,0)`, gripOffset은 0이다. 기본 가격과 size는 기획서의 상품 표를 사용한다. 저장 이후에는 id를 임의로 바꾸지 않는다.

## 작업실 프리팹 구성

### 계층과 좌표 기준

```text
Workshop                          WorkshopGame
  Room                            바닥, 천장, 벽, 조명
  Workbench                       천판, 다리, 강화판, TAKE, SELL
  Item mount - workbench          benchPivot
  SUPPLY                          왼쪽 거치대, 고정 피벗 6개
  STORAGE                         오른쪽 거치대, 고정 피벗과 클릭 면 6개
  NEXT ROUND                      BoxCollider, WorkshopStation
  Player look pivot               FirstPersonRig
    First person camera           Camera, AudioListener
    Engineer                      전신 모델과 본
      …hand.r
        Hammer grip socket
          UpgradeHammer
      …hand.l
        Item grip socket
  Workshop HUD                    Canvas, Text 4개
```

Room과 Workbench는 설명용 분류다. 기존 Generator는 많은 부품을 Workshop 직속으로 생성한다. 이름보다 Inspector에 연결한 참조를 사용한다. Workshop의 Position과 Rotation은 0, Scale은 1이다.

| 오브젝트 | Position | Scale 또는 크기 |
| --- | --- | --- |
| 바닥 | `(0,-0.1,0)` | `(6,0.2,6)` |
| 천장 | `(0,3.3,0)` | `(6,0.2,6)` |
| 좌우 벽 | `(±3,1.6,0)` | `(0.2,3.2,6)` |
| 앞뒤 벽 | `(0,1.6,±3)` | `(6,3.2,0.2)` |
| 천판 | `(0,0.94,1.5)` | `(2.6,0.18,1.2)` |
| 다리 4개 | `(±1.05,0.44,1.5±0.43)` | `(0.12,0.88,0.12)` |
| 강화판 | `(0,1.055,1.4)` | `(1.1,0.05,0.65)` |
| 작업대 상품 피벗 | `(0,1.19,1.4)` | `(1,1,1)` |
| TAKE | `(-0.95,1.09,1.04)` | `(0.42,0.12,0.28)` |
| SELL | `(1,1.07,1.45)` | `(0.48,0.08,0.55)` |
| NEXT ROUND | `(0,1.65,2.82)` | `(1.25,0.4,0.1)` |
| Player look pivot | `(0,1.6,-0.45)` | `(1,1,1)` |

피벗은 빈 GameObject로 만들며 Collider가 없다. 표의 나머지 공간 부품은 Cube로 만들고 BoxCollider를 유지한다. 실제 상품 부착 피벗은 크기를 변경한 Cube 밑에 넣지 않는다.

### 거치대

왼쪽 루트는 `(-2.25,0,0.35)`, Y 회전 -90도다. 오른쪽은 `(2.25,0,0.35)`, Y 회전 +90도다. 양쪽의 앞면이 방 중앙을 향해야 한다. 아래 값은 거치대 루트 기준 로컬 좌표다.

- 뒤판: 위치 `(0,1.5,0.2)`, 크기 `(2.4,2.25,0.14)`.
- 고정 피벗 index 0~5: x=`(index % 3 - 1) * 0.73`, y=`index < 3 ? 1.95 : 1.05`, z=0.
- 각 받침: 피벗 좌표에서 `(0,-0.3,0.07)`만큼 이동, 크기 `(0.62,0.08,0.32)`.
- 보관 클릭 면: 피벗 좌표에서 `(0,0,0.09)`만큼 이동, 크기 `(0.67,0.73,0.04)`.

상품이 없는 보관 칸에도 클릭 면의 Collider가 필요하다. 받침과 클릭 면은 피벗의 형제가 되어도 된다. supplyPivots와 storagePivots 배열 순서는 위 index와 일치시킨다.

### 스테이션 컴포넌트

WorkshopStation에는 kind, index, pivot을 둔다. kind는 `Bench, Supply, Storage, Sell, NextRound` 중 하나다.

| 부착 대상 | kind | index | pivot |
| --- | --- | ---: | --- |
| 천판과 강화판 | Bench | 0 | benchPivot |
| TAKE | Bench | 1 | benchPivot |
| SELL | Sell | 0 | 자신의 Transform |
| 보관 받침과 클릭 면 | Storage | 해당 칸 0~5 | 해당 storagePivot |
| NEXT ROUND | NextRound | 0 | 자신의 Transform |

공급 상품 선택은 상품 Collider를 직접 사용하므로 Supply 분기는 현재 사용하지 않는다. 현재 WorkshopGame은 스테이션의 pivot 대신 자신의 benchPivot과 storagePivots를 참조한다. 두 위치 정보가 서로 다르게 연결되지 않도록 한다.

## 1인칭 캐릭터와 손 리그

### 카메라와 레이어

Camera는 Player look pivot의 자식으로 두고 로컬 위치·회전을 0으로 한다. Tag=MainCamera, FOV=68, Near Clip=0.12, Far Clip=30, Clear Flags=Solid Color, 배경 RGB=`(0.015,0.02,0.025)`로 설정한다. AudioListener는 이 카메라에 하나만 둔다.

| 레이어 번호 | 용도 | 카메라 | 선택 Raycast |
| --- | --- | --- | --- |
| 0 | 공간, 상품, 스테이션 | 표시 | 포함 |
| 30 | 표시할 팔과 망치 | 표시 | 제외 |
| 31 | 머리·몸통·다리·장식, 숨긴 왼팔 | 제외 | 제외 |

Tags and Layers에서 30을 FirstPersonArms, 31을 PlayerBody로 이름 붙이면 편하다. 현재 구현은 번호로 처리하므로 이름은 필수가 아니다.

```csharp
camera.cullingMask = ~(1 << 31);
int interactionMask = ~((1 << 30) | (1 << 31));
```

Engineer를 look pivot 아래에 배치하고 로컬 위치를 `(0,-1.6,0)`으로 한다. 이름에 Arm이 포함된 Renderer의 GameObject는 30, 나머지 Renderer는 31로 지정한다. 부모에서 모든 자식 레이어를 한꺼번에 바꾸면 분리가 사라진다.

Scene 뷰에서는 30과 31을 모두 표시한다. 게임에서는 오른팔을 항상 보이고, `Engineer_ArmLeft`는 상품을 들었을 때만 30으로 바꾼다. 빈손일 때는 31로 되돌린다. Renderer 자체를 끄지 않으므로 편집 중 전신을 확인할 수 있다.

Animator는 비활성화하고 SkinnedMeshRenderer의 Update When Offscreen을 활성화한다. 팔과 망치의 Shadow Casting Mode는 Off로 한다. 캐릭터와 망치의 Collider는 제거한다.

### 본과 소켓 연결

| Rig 필드 | Engineer 본 또는 Renderer |
| --- | --- |
| rightUpper, rightLower, rightHand | `upperarm.r`, `lowerarm.r`, `hand.r` |
| leftUpper, leftLower, leftHand | `upperarm.l`, `lowerarm.l`, `hand.l` |
| leftArm | `Engineer_ArmLeft`의 Renderer |

hand.r 밑에 Hammer grip socket, hand.l 밑에 Item grip socket을 만든다. 소켓 로컬 위치·회전은 0, Scale은 1이다. 망치를 오른손 소켓 아래에 넣고 최대 길이가 약 0.4m가 되도록 정규화한다. 망치의 소켓 상대 위치는 `(0,0.18,0.03)`을 초기값으로 사용한다. 망치의 모든 자식은 레이어30이다.

### 팔 자세와 2본 IK

LateUpdate에서 어깨→팔꿈치→손의 위치를 계산한다. 다음 목표는 look pivot의 로컬 좌표를 TransformPoint로 변환해 사용한다.

```text
오른손 목표 = (0.32, -0.32 + strike*0.32, 0.58 - strike*0.12)
왼손 목표 = (-0.32, -0.35, 0.65)
오른쪽 굽힘 방향 = -look.up + look.right
왼쪽 굽힘 방향 = -look.up - look.right
오른손 회전 = look.rotation * Euler(-20 - strike*65, 0, 0)
왼손 회전 = look.rotation * Euler(0, 0, -20)
```

어깨 위치를 O, 위팔 길이를 a, 아래팔 길이를 b로 둔다. 목표 방향의 단위 벡터는 u다. 목표 거리 d는 `abs(a-b)+0.001`부터 `a+b-0.001`까지 제한한다. 굽힘 방향을 u에 수직인 평면으로 투영하고 정규화해 v를 구한다.

```text
h = (a*a + d*d - b*b) / (2*d)
elbow = O + u*h + v*sqrt(max(0, a*a - h*h))
```

Quaternion.FromToRotation으로 위팔을 현재 팔꿈치 방향에서 elbow 방향으로 회전하고, 아래팔을 현재 손 방향에서 목표 손 방향으로 회전한다. 손목 회전은 마지막에 지정한다. 본 참조와 길이가 올바른지 먼저 확인하고 상품을 부착한다.

## 입력과 선택 판정

InputAction을 코드에서 생성하는 방식이다. PlayerInput과 Input Actions 자산은 필수가 아니다.

| Action | Type | Binding | 담당 |
| --- | --- | --- | --- |
| Look | Value | `<Mouse>/delta` | FirstPersonRig |
| Rotate | Button | `<Mouse>/rightButton` | FirstPersonRig |
| Interact | Button | `<Mouse>/leftButton` | WorkshopGame |
| Retry | Button | `<Keyboard>/r` | WorkshopGame |

Rig는 OnEnable에서 Action 생성·Enable, OnDisable에서 Dispose한다. WorkshopGame은 Initialize에서 Enable하고 OnDestroy에서 Dispose한다. Rig 비활성화 시 Cursor 잠금을 풀고 포인터를 표시한다.

오른쪽 버튼을 누르는 동안에만 delta를 읽고 감도 0.13을 곱한다. yaw는 -105~105도, pitch는 -45~32도로 Clamp한 뒤 look pivot에 적용한다. 마우스 delta에 deltaTime을 다시 곱하지 않는다. 회전 중 CursorLockMode.Locked, 평상시 None을 사용한다.

좌클릭은 WasPressedThisFrame으로 처리하며, 우클릭 중이거나 busy이면 무시한다. 클릭과 hover는 아래 순서로 대상을 찾는다.

1. `camera.ScreenPointToRay(Mouse.current.position.ReadValue())`로 레이를 만든다.
2. 거리8, 레이어30/31 제외로 Physics.RaycastAll을 호출한다.
3. distance 오름차순으로 정렬한다. 반환 순서를 가정하지 않는다.
4. 손에 든 상품 자신의 Collider만 건너뛴다.
5. 처음 남은 충돌을 채택한다. 벽이나 뒤판이면 거기서 멈춘다.
6. GetComponentInParent로 WorkshopItem을 찾고, 없으면 WorkshopStation을 찾는다.

벽 뒤의 조작 대상을 계속 검색하면 벽을 관통해 선택하게 된다. 반대로 첫 Hit만 처리하면 손의 상품이 놓을 위치를 가로막을 수 있다. 여러 Hit를 정렬하고 손의 상품만 제외하는 이유다.

## 상품 생성과 위치 상태

### 인스턴스 구성

빈 ItemRoot에 WorkshopItem과 BoxCollider를 붙이고 상품 모델은 visual 자식으로 생성한다. definition을 설정하고 level=0, storageSlot=-1로 시작한다. 살아 있는 상품은 inventory 목록에 한 번씩만 등록한다.

손이나 거치대에 부착하기 전, 원점·Scale1인 ItemRoot 밑에서 전체 Renderer Bounds를 결합한다. 최대 변 길이가 L이면 `size/L`을 visual Scale에 곱한다. Bounds 중심에 같은 배율을 곱한 값만큼 visual 위치를 반대로 옮긴다. BoxCollider 각 변은 `max(bounds.size * scale, 0.12)`로 설정한다. 모델 내부의 기존 Collider는 제거한다.

회전·확대된 부모 아래에서 구한 월드 Bounds를 로컬 좌표로 바로 사용하지 않는다. 원점에서 정규화한 뒤 피벗에 부착하면 크기와 중심을 일관되게 계산할 수 있다. 상품은 고정 배치하므로 Rigidbody를 사용하지 않는다.

### Dock 공통 기능

```csharp
transform.SetParent(pivot, false);
transform.localPosition = offset;
transform.localRotation = Quaternion.Euler(euler);
```

공급·보관에는 displayEuler, 작업대에는 benchEuler, 손에는 gripEuler를 사용한다. 손잡이 부착은 정규화 때 중심으로 옮긴 visual 위치를 되돌리고 추가 보정을 더한다.

```csharp
offset = definition.gripOffset
       - Quaternion.Euler(definition.gripEuler) * visual.localPosition;
```

모델 원점이 손잡이와 다른 상품은 gripOffset으로 조정한다. 저장 복원에서도 동일한 계산을 사용한다.

### 상태 변경 계약

held는 손의 상품, bench는 작업대 상품이다. storageSlot이 0 이상이면 보관 중이고 -1이면 보관 외 위치다. 공급 슬롯은 상품의 부모를 supplyPivots에서 찾아 구한다.

| 동작 | 처리 |
| --- | --- |
| 공급·보관에서 집기 | held 지정, storageSlot=-1, 왼손 소켓에 Dock |
| 작업대에서 회수 | bench=null 후 held에 지정하고 왼손으로 Dock |
| 작업대 배치 | 빈 작업대 확인, bench=held, held=null, benchPivot에 Dock |
| 보관 | 슬롯 비어 있음 확인, storageSlot 지정, 보관 피벗에 Dock, held=null |
| 판매 | 현재 판매가 가산, inventory에서 제거, Destroy, held=null |
| 파괴 | inventory에서 제거, Destroy, bench=null |

동일 상품이 손과 작업대를 동시에 점유하거나 동일 보관 슬롯에 두 상품이 들어가지 않도록 한다. 손과 작업대는 각각 최대 1개다. TAKE는 Bench의 index1 분기로 처리한다. 작업대 상품을 직접 클릭하면 회수가 아니라 강화를 실행한다.

## 강화와 라운드

### 강화 코루틴

작업대 상품이 있고 최대 단계 미만이면 busy=true로 설정한다. 0.5초 동안 `strike = sin(Clamp01(elapsed/0.5) * PI)`를 갱신한다. 끝나면 strike=0, strikes를 1 증가시키고 난수 하나로 결과를 정한다.

```text
u = Random.value
s = 시도 전 단계의 성공률
d = 시도 전 단계의 파괴율

u < s       → level += 1
u < s+d     → 상품 파괴
나머지      → level = max(0, level-1)
```

성공과 파괴에 별도 난수를 사용하지 않는다. 결과 확정 후 저장하고 결과 조명과 메시지를 표시한다. 0.35초 뒤 조명 강도를0, busy=false로 되돌린다. +8에서는 코루틴 진입 직후 안내만 하고 시도 횟수를 증가시키지 않는다.

망치와 상품의 실제 물리 충돌로 추첨하지 않는다. 손 움직임은 연출이고 결과는 규칙 처리다. 이후 애니메이션이나 소리를 추가해도 확률 계산과 분리해 유지한다.

### 라운드 전환과 리필

round=1, revenue=0, strikes=0으로 시작한다. NEXT ROUND는 `revenue >= Target(round)`이고 held와 bench가 모두 null일 때만 실행한다. round를1 증가시키고 revenue와 strikes를0으로 만든 뒤 Refill과 Save를 실행한다.

Refill은 inventory의 복사본을 순회하며 storageSlot<0인 상품만 삭제한다. 삭제하면서 원본 List를 foreach하지 않는다. held와 bench를 비우고 supplyPivots 순서대로 기본 상품 6개를 +0으로 생성한다. 보관 상품은 그대로 둔다.

실패 조건은 `inventory.Count == 0 && revenue < Target(round)`다. R은 이때만 허용한다. 현재 round를 유지하고 revenue와 strikes를0으로 초기화한 뒤 리필한다. 목표를 달성한 상품0 상태는 실패가 아니다.

## UI와 내부 조명

Canvas는 Screen Space Overlay다. CanvasScaler는 Scale With Screen Size, 기준1600×900, Match0.5로 설정한다. Text4개의 Raycast Target을 끄고 어두운 Outline을 추가한다. 기본 폰트는 LegacyRuntime.ttf이며 일본어·한국어 등 추가 글리프가 필요하면 해당 글리프를 포함한 폰트를 준비한다.

| 연결 필드 | Anchor/Pivot | Anchored Position | Size | Font Size |
| --- | --- | --- | --- | ---: |
| statusText | `(0,1)` | `(32,-25)` | `(1100,90)` | 25 |
| itemText | `(0,0)` | `(32,105)` | `(1450,85)` | 24 |
| helpText | `(0,0)` | `(32,24)` | `(1450,65)` | 19 |
| messageText | `(0.5,1)` | `(0,-130)` | `(1450,60)` | 23 |

상품 표시는 hover→bench→held 순으로 선택한다. 메시지는 실패→유효한 임시 메시지→목표 달성→기본 안내 순으로 표시한다. 이 UI는 Button이 없고3D Collider를 조작하므로 EventSystem은 필요하지 않다. UI 버튼을 추가한다면 InputSystemUIInputModule을 사용하고 화면 뒤3D 대상과 클릭이 겹치지 않게 한다.

| 조명 | Position | 색 RGB | Intensity | Range | 설정 |
| --- | --- | --- | ---: | ---: | --- |
| 작업등 Spot | `(0,2.85,1.2)` | `(1,0.73,0.42)` | 9 | 6 | X회전90, Angle76 |
| 공급 Point | `(-1.9,2.3,0.3)` | `(1,0.62,0.28)` | 3 | 3.2 | 따뜻한 상품 조명 |
| 보관 Point | `(1.9,2.3,0.3)` | `(0.25,0.7,1)` | 3 | 3.2 | 차가운 보관 조명 |
| 보조 Point | `(0,2.2,-0.7)` | `(0.5,0.6,0.7)` | 0.8 | 4 | 시인성 보완 |
| 결과 Point | `(0,1.8,1.4)` | 결과별 변경 | 보통0, 결과4 | 2 | resultLight 연결 |

환경광은 Flat, RGB `(0.045,0.055,0.065)`다. Skybox를 제거한다. Fog는 Exponential Squared, RGB `(0.015,0.02,0.025)`, Density0.055다. 방은 URP/Lit 단색 재질부터 구성한다. 벽은 짙은 회청색, 천판은 갈색, 받침은 검은 금속, TAKE는 황동, SELL과 보관대는 청록색으로 구분한다. 표지는 TextMesh로 면에서 조금 띄워 배치한다.

## Inspector 연결과 Bootstrap

Workshop 루트의 WorkshopGame에 아래 필드를 모두 연결한다.

| 필드 | 참조 |
| --- | --- |
| rig | Player look pivot의 FirstPersonRig |
| benchPivot | 작업대 상품 피벗 |
| supplyPivots | 왼쪽 피벗0~5 |
| storagePivots | 오른쪽 피벗0~5 |
| resultLight | 결과 Point Light |
| statusText/itemText/helpText/messageText | 대응하는 Canvas Text |

Rig에도 view, 양팔 본6개, leftArm, hammerGrip, itemGrip을 연결한다. Workshop을 Prefabs/Workshop.prefab으로 저장하고 설정 자산의 workshop에 지정한다. 설정 items의6개 prefab도 연결한다.

Bootstrap 씬에는 Bootstrap 컴포넌트를 가진 오브젝트와 편집용 Workshop 프리뷰를 둔다. Bootstrap의 settings는 Resources/ReinforceSettings.asset, editorPreview는 프리뷰 인스턴스에 연결한다.

Awake에서는 프리뷰를 먼저 SetActive(false)하고 Destroy한다. 환경광과 안개를 설정하고 settings를 확인한다. 참조가 없으면 `Resources.Load<ReinforceSettings>("ReinforceSettings")`로 읽는다. Resources 경로에는 폴더명 Resources나 확장자를 포함하지 않는다.

settings 또는 workshop이 없으면 오류를 출력하고 초기화를 중지한다. 정상이라면 settings.workshop을 Instantiate하고 그 루트의 WorkshopGame.Initialize(settings)를1회 호출한다. Initialize는 입력을 켜고 저장 복원에 성공하면 복원 상태를, 저장이 없거나 읽지 못하면 새 공급 상태를 만든다. 초기화 전 Update는 아무 작업도 하지 않는다.

프리뷰를 남긴 채 실행 프리팹을 또 만들면 카메라·조명·리스너가 중복된다. 편집용 전체 모델과 데이터로 시작하는 실행 모델을 교체하는 이유다.

## 저장 형식과 복원

JsonUtility로 직렬화 가능한 일반 클래스를 JSON으로 변환해 PlayerPrefs의 `ReinforceGame.Save.v1`에 저장한다. 객체 참조 대신 상품 id, 단계, 위치와 슬롯을 저장한다.

```json
{
  "round": 2,
  "revenue": 0,
  "strikes": 0,
  "items": [
    { "id": "axe_1handed", "level": 3, "location": "storage", "slot": 0 },
    { "id": "sword_1handed", "level": 0, "location": "supply", "slot": 0 }
  ]
}
```

items는 설명용 발췌다. 실제로는 살아 있는 모든 상품을 포함한다. location 허용값은 supply, hand, bench, storage다. supply와 storage의 slot은0~5, hand와 bench는-1이다.

복원은 JSON 읽기→round와items 확인→id를 설정에서 검색→슬롯 범위 확인→상품 생성→level 범위 보정→부모와held/bench/storageSlot 복원 순서다. 손에 복원할 때도 앞의 손잡이 보정식을 사용한다.

현재 Load는 알 수 없는id와 공급·보관 슬롯 범위를 검사한다. 확장 시 location 허용값, 슬롯 중복, 손·작업대 복수 등록, 음수 금액, 비정상 round도 생성 전에 검사한다. 형식 변경 시 저장 키 버전을 올리거나 이전 형식 이동 처리를 추가한다.

저장은 위치 이동·판매·강화 결과·라운드 전환·재도전 완료 시점에 한다. SetString 후 PlayerPrefs.Save를 호출한다. Reset Saved Progress는 해당 키만 삭제한다. 초기화된 새 게임을 보려면 Play를 다시 시작한다.

## Editor 생성 도구

위에서 만든 구조를 반복 제작할 수 있도록 WorkshopGenerator를 작성한다. 이 도구의 출력은 씬·프리팹·설정·재질 자산이다. 동작 코드는 앞의 Runtime 스크립트로 작성한다.

1. Play 상태를 검사하고 출력 폴더를 준비한다.
2. 선택한 모델과 의존 파일을 GUID를 유지하며 이동한다. 이동됐으면 목적지 자산을 사용한다.
3. 이름 없는 현재 씬은 먼저 저장한다. 기존 씬을 기억하고 제작용 씬을 Additive로 생성한다.
4. 재질, 방, 작업대, 거치대, 캐릭터, 카메라, 조명, HUD를 구성한다.
5. 모든 컴포넌트 참조와 상품 설정을 연결한다.
6. PrefabUtility.SaveAsPrefabAsset으로 Workshop을 저장한다.
7. Bootstrap과 프리뷰를 배치하고 EditorSceneManager.SaveScene으로 시작 씬을 저장한다.
8. 설정 자산을 SetDirty하고 AssetDatabase.SaveAssets를 호출한다.
9. 제작 씬을 닫고 기존 씬으로 돌아와 참조·피벗 수·카메라 필터·의존성을 검증한다.

`[MenuItem("Reinforce Game/Generate Workshop")]`으로 실행 메뉴를 만든다. 기존 메뉴에는 Open Bootstrap, Reset Saved Progress, Validate Generated Assets도 있다. 재생성은 Workshop과Bootstrap 씬을 덮어쓰고 상품 정의를 기본값으로 다시 채운다. 유지할 배치 변경은 Generator에도 반영한다.

## 단계별 완성 기준

| 단계 | 구현할 부분 | 확인 결과 |
| --- | --- | --- |
| 1 | 설정·상품·스테이션 타입 | 컴파일, 설정 자산 생성과 편집 가능 |
| 2 | 방·작업대·거치대·카메라 | Game뷰에 공간과 좌우6개 슬롯 표시 |
| 3 | Engineer·레이어·손·망치 | Scene에서 전신, Game에서 오른손과 망치 표시 |
| 4 | 입력과Raycast | 우클릭 시점 회전, 올바른 대상 판정 |
| 5 | Spawn·Pickup·Place·TAKE | 손→작업대→손 이동, 복제나 유실 없음 |
| 6 | 강화·UI | 성공·하락·파괴, 가격 갱신, 연타 중복 방지 |
| 7 | 판매·보관·라운드·실패 | 금액 계산, 슬롯 점유, 이월과 리필 |
| 8 | 저장과Bootstrap | 실행 재개, 프리뷰 중복 방지 |
| 9 | Editor 재생성과빌드 | 참조 유지, 시작 씬으로 정상 플레이 |

Play 중 씬 인스턴스 변경은 정지하면 되돌아간다. ScriptableObject 자산의 Play 중 변경은 유지될 수 있으므로 테스트 설정을 복제하거나 원래 값을 복구한다. 조정한 배치는 정지 후 프리팹이나 Generator에 반영한다. Build Profiles의Scene List에Bootstrap 씬을 넣고 첫 씬으로 지정한다.

## 검증과 문제 해결

확률 분기는 검증용 설정 복사본에서 고정한다. 성공률1/파괴율0은 성공, 성공률0/파괴율0은 하락, 성공률0/파괴율1은 파괴다. 검사 후 본래 설정으로 되돌린다.

| 시험 | 기대 결과 |
| --- | --- |
| 새 저장으로 실행 | round1, 판매액0, 공급6, 보관0 |
| 손의 상품이 포인터 앞에 있음 | 자신의Collider를 제외하고 배치 대상 선택 |
| 벽 뒤 상품 클릭 | 벽에서 판정 종료 |
| 찬 보관 칸에 배치 | 기존 상품과 손의 상품 유지 |
| +0 하락 | +0 유지 |
| 파괴 | bench=null, inventory1개 감소 |
| 강화 중 연타 | 시도1회만 기록 |
| +8 추가 시도 | 단계와시도 수 불변 |
| 판매 | 현재 가격만큼 증가 후 상품 제거 |
| 목표 미달 NEXT ROUND | round 불변 |
| 목표 달성, 작업대에 상품 존재 | 정리 전 진행 불가 |
| +3 보관 후 다음 라운드 | +3 유지, 공급6개+0으로 리필 |
| 전 상품 소진과목표 미달 | R로 같은round 재도전 |
| 손·작업대·보관 상태 저장 | 다시 실행해 종류·단계·위치 복원 |

기존 Validate Generated Assets는 구조를, WorkshopSmokeTest.Run은 검증용 Editor의 플레이 분기를 검사한다. Validation/smoke-pass.txt와 카메라 PNG는 이전 검증 결과다. 코드를 수정하면 관련 항목을 다시 확인한다.

| 증상 | 확인할 부분 |
| --- | --- |
| 몸통이나 머리가 보임 | Renderer별 31 지정과 Culling Mask |
| 손과 망치도 안 보임 | 30 표시 여부, 자식 일괄 레이어 설정, Near Clip |
| 팔이 안 움직임 | 본 이름과 참조, Animator 경쟁, Update When Offscreen |
| 빈 보관 칸이 클릭되지 않음 | 클릭 면 Collider, index, 뒤판에 가려졌는지 |
| 손에 든 뒤 내려놓지 못함 | 손의 상품 Raycast 제외, held/bench 상태 |
| 모델 크기나 중심이 틀림 | 부모 Scale, 정규화 시점, Bounds와 visual 좌표 |
| 분홍색 모델 | URP 재질, 파이프라인, 이동된 재질 참조 |
| 지나치게 어두움 | Additional Lights, Range, 보조광과 재질 색 |
| 입력 없음 | Active Input Handling, Action.Enable, Game 뷰 포커스 |
| 카메라와 빛 중복 | Bootstrap 중복, 프리뷰 비활성화, Listener 수 |
| 첫 라운드로 시작하지 않음 | 저장 복원과 Reset Saved Progress |

현재 WorkshopGame은 작은 게임을 위한 통합 구성이다. 규모가 커지면 추첨 규칙, 라운드 상태, 저장을 일반 C# 클래스로 분리해 각각 검증할 수 있다. 목표액은 지수 증가하지만 공급 수와 최대 단계는 고정이므로, 장기 진행에는 기획서의 라운드 상한 또는 목표 증가 정책을 추가로 결정해야 한다.
