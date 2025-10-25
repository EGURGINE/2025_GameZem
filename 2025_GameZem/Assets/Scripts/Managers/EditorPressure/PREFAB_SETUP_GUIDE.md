# 편집자 원고 독촉 Prefab 설정 가이드

## 📋 개요
이 가이드는 편집자 원고 독촉 이벤트를 위한 Spine SkeletonGraphic Prefab을 생성하는 방법을 설명합니다.

---

## 🎨 Prefab 생성 단계

### 1. Canvas 하위에 새 GameObject 생성
1. Hierarchy에서 **Canvas** 하위에 우클릭
2. **Create Empty** 선택
3. 이름을 `EditorPressure`로 변경

### 2. SkeletonGraphic 컴포넌트 추가
1. `EditorPressure` GameObject 선택
2. Inspector에서 **Add Component** 클릭
3. `SkeletonGraphic` 검색 후 추가
4. **Skeleton Data Asset** 필드에 편집자 skeleton 데이터 할당
   - 예: `Resources/Spine/editor_pressure/editor_pressure_SkeletonData`

### 3. Spine 애니메이션 설정
SkeletonGraphic의 **Initial Skin**과 **Animation** 설정:
- **Initial Skin**: 기본 스킨 (있으면)
- **Start Animation**: 비워두기 (스크립트에서 제어)

**필요한 애니메이션:**
- `appear` - 등장 애니메이션 (한 번 재생)
- `idle` - 대기 애니메이션 (루프)
- `disappear` - 퇴장 애니메이션 (한 번 재생)

### 4. EditorPressureController 스크립트 추가
1. `EditorPressure` GameObject 선택
2. Inspector에서 **Add Component** 클릭
3. `EditorPressureController` 검색 후 추가

### 5. 스크립트 설정
EditorPressureController의 Inspector 설정:

#### Spine Animation
- **Skeleton Graphic**: 자동으로 채워짐 (동일 GameObject의 SkeletonGraphic)

#### Animation Names
- **Appear Animation Name**: `appear` (Spine 프로젝트의 실제 애니메이션 이름)
- **Idle Animation Name**: `idle`
- **Disappear Animation Name**: `disappear`

#### Settings
- **Display Duration**: `0` (무한정, 토글 시까지 유지)

### 6. RectTransform 설정
`EditorPressure`의 RectTransform 설정:
- **Position**: 원하는 화면 위치 (예: X=0, Y=200, Z=0)
- **Width/Height**: Spine 그래픽 크기에 맞게 조정
- **Anchors**: 화면 중앙 고정 추천 (Center-Center)
- **Scale**: 적절한 크기로 조정 (예: X=1, Y=1, Z=1)

### 7. Prefab 생성
1. Hierarchy의 `EditorPressure` GameObject를 드래그
2. `Resources/Prefabs/` 폴더로 드롭
3. Prefab 파일 이름 확인: `EditorPressure.prefab`

### 8. ObstacleManager에 연결
1. Hierarchy에서 **ObstacleManager** GameObject 선택
2. Inspector의 **Obstacle Prefabs** 섹션 찾기
3. **Editor Pressure Prefab** 필드에 방금 생성한 Prefab 할당

---

## ⚙️ 애니메이션 플로우

```
게임 시작
    ↓
EditorPressure 이벤트 발생
    ↓
Prefab Instantiate
    ↓
[appear] 애니메이션 재생 (1회)
    ↓
OnSpineAnimationComplete 이벤트
    ↓
[idle] 애니메이션 재생 (루프)
    ↓
(편집자 이벤트 토글 - 종료)
    ↓
[disappear] 애니메이션 재생 (1회)
    ↓
OnSpineAnimationComplete 이벤트
    ↓
SetActive(false) → Destroy
```

---

## 🔧 CutSpawner 속도 시스템

### 일반 상황
- 기본 속도: `baseCutSpeed = 10f`
- 시간에 따라 점진적 증가
- **상한선**: `normalSpeedCap = 2.5f` (최대 2.5배)

### 편집자 원고 독촉 활성화 시
- **상한선 돌파**: `editorPressureSpeedCap = 4.0f` (최대 4배)
- 속도가 계속 증가 가능

### 편집자 원고 독촉 비활성화 시
- 상한선이 다시 `normalSpeedCap`으로 복귀
- 현재 속도가 상한선 초과 시 자동으로 `normalSpeedCap`으로 조정

---

## 📝 스테이지 데이터 예시

편집자 원고 독촉은 다음 시기에 발생합니다:

```csharp
// 2007_1: 시작
new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)

// 2007_2: 종료 (토글)
new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)

// 2007_6: 재시작
new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)

// 2007_7: 종료 (토글)
new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)

// 패턴 반복...
```

---

## 🐛 트러블슈팅

### 애니메이션이 재생되지 않는 경우
1. Spine 데이터 에셋이 올바르게 할당되었는지 확인
2. 애니메이션 이름이 정확한지 확인 (대소문자 구분)
3. Console에서 에러 메시지 확인

### Prefab이 화면에 표시되지 않는 경우
1. Canvas의 Render Mode 확인 (Screen Space - Overlay 권장)
2. RectTransform의 Position 확인
3. SkeletonGraphic의 Material 확인

### 속도가 증가하지 않는 경우
1. CutSpawner의 `speedIncreaseRate` 값 확인 (기본 0.02)
2. Console에서 `[Editor Pressure] STARTED!` 메시지 확인
3. `GetCurrentDifficulty()` 메서드로 현재 속도 배수 확인

---

## 📞 추가 정보

- **스크립트 위치**: `Scripts/Managers/EditorPressure/EditorPressureController.cs`
- **Prefab 저장 위치**: `Resources/Prefabs/EditorPressure.prefab`
- **관련 스크립트**: `CutSpawner.cs`, `ObstacleManager.cs`

