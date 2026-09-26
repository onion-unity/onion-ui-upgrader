# Copilot review instructions

리뷰 코멘트는 한국어로 작성한다. 스타일 지적보다 동작 버그, Unity 특유의 함정, 아래 설계 규칙 위반을 우선한다.

## 프로젝트

- `com.onion.uiupgrader`: UGUI `Selectable`을 상속하지 않고 네비게이션을 개선하는 Unity 6 (6000.0) UPM 패키지.
- 자동 테스트/CI 없음. 검증은 Unity Editor Play Mode에서 수동으로 한다. 그래서 리뷰가 유일한 자동 검사다.
- `Runtime/` (`Onion.UI.Runtime`), `Editor/` (`Onion.UI.Editor`, Editor 전용). 네임스페이스는 폴더별 (`Onion.UI.Navigation` 등).

## 코드 규칙

- K&R 브레이스, 4칸 들여쓰기, private 필드 `_camelCase`.
- 비동기는 `Awaitable` 사용 (`Task`/코루틴 X).
- `UnityEditor` 참조는 `Editor/` 안에서만. Runtime 파일에서 필요하면 `#if UNITY_EDITOR`로 감싼다.
- 선택적 의존성은 asmdef `versionDefines`로 게이팅 (예: Input System은 `#if ONION_INPUTSYSTEM`). 패키지 존재 여부를 다른 방법으로 검사하지 않는다.
- `Onion.UI.Navigation` 네임스페이스는 `UnityEngine.UI.Navigation`을 가리므로 alias `UINavigation`을 쓴다.
- 직렬화 필드 이름을 바꾸면 `[FormerlySerializedAs]`가 있어야 한다.

## 설계 불변식 (위반 시 지적)

- `Selectable`을 상속하지 않는다. Button/Toggle/Slider 등이 이미 상속하고 있기 때문.
- `NavigationUpgrader`는 선택된 Selectable 하나의 `navigation`만 Explicit으로 바꾸고, 선택이 바뀌면 원래 값을 복원한다. 씬 데이터를 영구히 바꾸지 않는다.
- Explicit/None 모드 Selectable은 건드리지 않는다. Automatic/Horizontal/Vertical만 대상.
- 이웃 점수 계산은 전부 `NeighborSearch`에 있다 (런타임과 Editor 프리뷰가 공유). 다른 곳에 점수 로직을 복제하지 않는다.
- 점수 공식 `distance / cos(angle)^k`, `k = alignmentBias × 4`는 `alignmentBias 0.25`, `directionTolerance 90°`에서 Unity `Selectable.FindSelectable`과 정확히 같아야 한다 (wrap-around 포함). 이 동치를 깨는 변경은 지적한다.
- 업그레이드는 기본 꺼짐(opt-in). on/off 판단은 `NavigationSettings.profile`(null이면 꺼짐) 하나로 한다. 내장 fallback 프로필은 의도적으로 없다.
- Enter Play Mode에서 도메인 리로드가 꺼져 있어도 동작해야 한다. 리셋되지 않는 static 상태를 새로 추가하면 지적한다 (인스턴스 상태나 명시적 초기화 사용).
- `NavigationGroup`은 `[ExecuteAlways]`로 Edit Mode에서도 등록된다. Play Mode 전용 동작은 `Application.isPlaying`으로 막아야 한다.

## 성능

- `NavigationUpgrader.Update`는 매 프레임, `EventSystem.Update`보다 먼저 (`DefaultExecutionOrder(-10000)`) 실행된다. 이 경로에서 매 프레임 GC 할당(LINQ, 클로저, `GetComponents` 배열 반환, 문자열 연결)이나 불필요한 네이티브 호출을 지적한다.
- 후보 rect는 `Resolve` 한 번에 한 번만 계산해 캐시한다. 캐시를 우회하는 변경을 지적한다.

## Editor 코드

- Scene view/인스펙터 그리기 코드에서 `Handles.color` 등 전역 상태를 바꾸면 복원해야 한다.
- 에셋을 자동 생성하는 코드는 batch mode에서 실행되면 안 된다.

## 기타

- 공개 API나 동작이 바뀌면 `README.md` 갱신과 `package.json` `version` 올림이 필요한지 확인한다.
