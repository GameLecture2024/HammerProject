ReinforceGame / THE LAST FORGE

실행: Assets/ReinforceGame/Scenes/Bootstrap.unity를 열고 Play.
Bootstrap은 ReinforceSettings 데이터를 읽고 Editor에서 생성한 Workshop 프리팹을 생성한다.
씬의 Editor Preview는 실행 직전에 비활성화되고 실제 데이터 기반 인스턴스로 교체된다.
Scene 뷰에는 Engineer의 전신이 있으며 게임 카메라는 몸통 레이어 31을 제외한다.
팔/망치는 레이어 30이다. 프로젝트의 레이어 이름 설정을 변경할 필요는 없다.

조작
- 우클릭 드래그: 좌우/상하 시점 회전. 마우스를 놓으면 포인터로 선택.
- 왼쪽 SUPPLY의 상품 좌클릭: 왼손으로 집기.
- 작업대 좌클릭: 손의 상품 올리기.
- 작업대 상품 또는 REINFORCE 좌클릭: 강화.
- TAKE 좌클릭: 작업대 상품 회수.
- 상품을 든 상태로 SELL 트레이 좌클릭: 판매.
- 오른쪽 KEEP 거치대의 빈 칸 좌클릭: 보관. 보관 상품 클릭: 다시 집기.
- 목표 판매액 달성 후 NEXT ROUND 클릭: 다음 라운드.
- 전 상품 소진 + 목표 미달이면 R: 현재 라운드 재도전.

규칙
강화 횟수와 비용 제한은 없다. 강화 1회마다 성공/하락/파괴 중 하나가 발생한다.
0단계에서 하락하면 0단계를 유지한다. 최대 단계는 기본 +8.
기본 1라운드 목표 250, 매 라운드 1.45배. 판매액은 라운드마다 초기화된다.
진열대 상품 6개는 다음 라운드에 리필된다. 미사용 공급 상품은 이월되지 않는다.
보관 상품은 레벨 그대로 이월된다. 작업대/손에 있는 상품은 판매/보관해야 라운드를 넘길 수 있다.
판매·강화·집기·배치·라운드 전환 때 진행 상태를 PlayerPrefs에 자동 저장한다.
게임 종료 후에도 진행을 복원한다. 초기화 메뉴: Reinforce Game > Reset Saved Progress.

데이터 수정
Resources/ReinforceSettings.asset: 목표액, 증가율, 단계별 성공/파괴율, 가격 배율, 상품과 피벗 보정.
하락 확률 = 1 - 성공 확률 - 파괴 확률. 파괴율은 남은 확률 안으로 제한된다.
상품 데이터의 displayEuler/gripEuler/gripOffset/benchEuler로 진열·손잡이·작업대 방향을 조정한다.
Products 프리팹과 Art/Characters 원본은 GUID를 보존해 선정 에셋과 의존 파일을 이동한 것이다.

Editor 도구
Reinforce Game > Generate Workshop: 모델 선별 이동, 재질·방·조명·프리팹·Bootstrap 씬 생성.
재생성 전 현재 씬을 저장한다. 수작업 씬 수정은 재생성 시 덮어쓰므로 Generator에서 변경한다.
Reinforce Game > Open Bootstrap: 시작 씬 열기.
Reinforce Game > Validate Generated Assets: 참조, 피벗, 카메라 필터, 외부 의존성 검증.
이름 없는 레이어 30/31은 이 게임의 팔/몸 구분용으로 예약한다.

검증
Validation 폴더에 Editor 생성 검증 결과와 플레이 테스트 결과/화면을 저장한다.
WorkshopSmokeTest.Run은 배치 Editor에서 시작하여 Bootstrap, 강화 결과 3종,
하락 하한, 판매, 회수, 목표 미달 차단, 보관 이월, 리필, 저장 복원을 검증한다.
이 테스트는 기존 저장 진행을 백업하고 종료 전에 복원한다.
