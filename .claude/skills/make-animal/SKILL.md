---
name: make-animal
description: 첨부 이미지(또는 말로 한 설명)를 분석해 AnimalRun 러너 동물을 Quirky 스타일로 블렌더에서 모델링하고 Unity 러너로 등록한다. "이 이미지로 동물 만들어줘", "○○ 러너 추가해줘", "노란 유니콘 만들어줘"처럼 새 동물 모델을 요청할 때 사용.
---

# 이미지 → 러너 동물 만들기

AnimalRun(Unity 2023.1.17f1)의 러너 동물을 원본 Quirky Series 규격으로 만든다. 답변은 한국어로.
사진을 3D로 그대로 옮기는 게 아니라, 이미지를 보고 **사양서(JSON)** 를 쓴 뒤 `make_animal.py`로 Quirky 스타일로 다시 만든다.
몸통·다리·머리는 비슷한 원본이 있으면 가져다 쓰고, 이미지의 특징 부위는 원본 스타일에 맞춰 새로 붙인다. 색만 바꾸고 끝내거나 원본 스타일을 벗어나지 않는다.

먼저 읽을 파일
- `ArtSource/Blender/specs/FORMAT.md`: 사양서 형식 (필수)
- `ArtSource/Blender/README.md`: 원본 스타일 규칙, 팔레트 슬롯 표, Unity 연결
- `ArtSource/Blender/specs/examples/`: Unicorn.json(reference_mesh, 기본), Pig.json(procedural, 맞는 원본이 없을 때)
- `ArtSource/Blender/references/animals_overview.png`, `palettes.png`: 원본 9종 모습과 팔레트

## 1. 이미지 분석
이미지(없으면 사용자 설명)에서 정리한다.
- 동물 종류와 표시 이름(한국어), 영문 `name`(PascalCase, 기존 러너와 겹치지 않게: `Assets/Resources/Animal Run/Prefabs/Runners/` 확인)
- 체형: 몸통 길이/높이, 다리 길이, 목 길이, 머리 크기 비율
- 특징 부위: 뿔, 갈기, 날개, 큰 귀, 볏, 혹, 꼬리 모양 등. **있는 것과 없는 것을 둘 다 적는다**(예: "뿔 없음"). 애매하면(귀인지 뿔인지 등) 없는 쪽으로 본다
- 색: 몸 주색·밝은색·어두운색, 다리, 발굽, 포인트(갈기·뿔·코). 사진 색 그대로보다 원본처럼 **채도 있는 파스텔 톤**으로 단순화
- 크기 분류 `kind`: 쥐사슴·돼지 Small, 말·얼룩말 Medium, 낙타·기린·페가수스 Large

분석 결과를 사용자에게 짧게 말하고 바로 진행한다(확인을 기다리지 않음). 이미지가 동물이 아니거나 너무 모호할 때만 묻는다.

## 2. 만드는 방식 고르기
원칙 네 가지.
- **이미지에 없는 부위는 넣지 않는다.** 기반 원본에 있는 부위라도(페가수스 뿔, 기린 뿔, 누 뿔 등) 이미지에 없으면 반드시 지운다. 이미지에 없는 장식(가시, 앞머리 등)을 임의로 더하지도 않는다.
- **이미지에 있는 부위는 빠뜨리거나 바꾸지 않는다.** 위치·개수·크기까지 이미지대로 만든다. 기반 원본에 비슷한 부위가 있어도 위치·개수·크기가 다르면 색만 바꿔 대신하지 말고, 원본 부위를 지운 뒤 이미지 모양대로 새로 만든다.
  예: `specs/WhiteKirin.json`. 이미지는 이마 가운데 큰 가지뿔 하나인데 Kirin 뿔은 머리 양옆의 작은 뿔 2개라서, 원본 뿔은 `repaint` + `"delete": true`로 지우고 가운데 가지뿔을 tube로 새로 만들었다.
- **원본 Quirky 스타일을 벗어나지 않는다.** 둥글고 단순한 덩어리, 막대 다리, 큰 원형 눈, 면 단위 단색. 사진의 사실적인 디테일(가닥가닥 깃털, 근육, 긴 다리 비율)은 원본 수준으로 단순화한다.
- **색만 바꾸고 끝내지 않는다.** 이미지의 특징 부위(날개 모양, 갈기, 꼬리, 뿔, 털 등)는 반드시 모양으로 반영한다.

### 기본: 원본 몸 + 특징 부위 교체 (`reference_mesh`)
원본 9종 중 몸통·다리·머리 체형이 비슷한 게 있으면 그 LOD0를 가져다 쓴다(손으로 다듬은 원본이라 품질이 가장 좋음).
그다음 이미지와 다른 부위만 고친다.
- 맞지 않는 부위 지우기: `remove_mesh`(메시만 지우고 본은 남김, 그 자리에 새 부품을 붙일 때), `remove_parts`(본까지 없앰), `remove_slots`(뿔처럼 본이 따로 없는 부위)
- 크기 조절: `part_scale` (귀, 꼬리 등. 다리는 굵기만)
- 새 부위 붙이기: `parts` (`merge: false`). 원본 부위와 비슷한 두께·둥글기로, 덩어리 2~5개 정도로 단순하게
- 지운 부위의 본을 새 부품에 맞게 옮기려면 `bones` (reference_mesh에서는 지운 부위의 본만 옮긴다. 남은 원본 메시가 붙은 본은 건드리지 않음)
- 원본 부위가 이미지에도 있고 위치·개수·크기·모양이 비슷하면 지우지 말고 그대로 두고 색만 맞춘다(갈기 등). 하나라도 다르면 지우고 새로 만든다
- 본·팔레트 칸이 얼굴과 같아 `remove_slots`로 못 가르는 부위(Kirin 뿔 등)는 `repaint`에 `z_min`/`z_max`와 `"delete": true`를 써서 지운다
- 원본 부위가 이미지에 없으면 지운다. 사양서 쓰기 전에 기반 원본의 부위 목록(LOD0 본 그룹·팔레트 칸)과 1단계의 "없는 것"을 대조한다
- 말 계열 원본: Horse(갈기 짧음), Zebra, Pegasus(뿔 + 날개, 뿔은 5아래 칸), Kirin(뿔·갈기 풍성), Llama/Camel(긴 목), Wildebeest(뿔·처진 머리), Mousedeer(작고 다리 짧음)
- 각 원본이 팔레트 칸을 어디에 쓰는지, 부위 좌표가 어디인지는 블렌더로 확인한다. 리그 본 위치와 LOD0 범위를 출력하거나, 면별 UV 칸을 센다(칸이 곧 부위 색).
- 예시: `specs/examples/Unicorn.json`(날개 제거 + 색), `specs/DarkPegasus.json`(이미지에 없는 뿔 제거, 날개를 세운 새 날개로 교체, 발목 털 추가, 꼬리 키움)

### 맞는 원본 체형이 없을 때만: `procedural`
돼지, 곰, 고양이처럼 원본 9종과 몸 구조가 다르면 `parts`로 몸을 새로 만든다(`specs/examples/Pig.json`).
이때도 Quirky 체형(큰 원통형 몸통, 짧은 목, 큰 머리, 막대 다리)과 원본 크기 범위(길이 1.1~2.1, 높이 0.74~1.56)를 지킨다.
- `base.reference`는 리그·애니메이션 18개를 가져오는 출처. 날개가 있으면 Pegasus(`wing.L`/`wing.R` 본과 Fly 동작이 있는 유일한 리그)
- `face_kit.reference`는 눈 모양이 맞는 원본(보통 Horse)
- 몸통·목·머리·주둥이·귀는 `merge: true` 도형을 겹쳐 한 덩어리로, 다리는 `cylinder`(`tip_slot`으로 발굽), 꼬리는 `tube`
- 다리 4개, 꼬리 2마디, 귀 2개(+날개 2개) 구조를 벗어나는 동물(뱀, 문어, 새)은 품질 한계를 사용자에게 먼저 알린다.

### 공통
- `bones`를 옮길 때 본의 **방향**은 reference 원본과 비슷하게 둔다(위치만 옮김). 애니메이션은 본 기준 회전이라, 방향을 바꾸면 회전축이 틀어진다.
  예: Pegasus는 tail.01 뒤로 수평 → tail.02 아래, wing 본은 어깨에서 뒤쪽 약간 아래로
- 날개 부품은 `bone: "wing.L"`/`"wing.R"`, 날개 뿌리는 wing 본 머리에. 원본 페가수스 날개처럼 두툼하고 둥근 깃 3~5개로
- 팔레트 6아래(입속)·7·8은 공용색이라 쓰지 않는다.

## 3. 사양서 작성 → 실행
`ArtSource/Blender/specs/<Name>.json`에 쓰고 실행한다(저장소 루트에서).
```
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --factory-startup --python ArtSource/Blender/tools/make_animal.py -- ArtSource/Blender/specs/<Name>.json
```
블렌더가 그 경로에 없으면 설치 위치를 찾고, 없으면 사용자에게 winget 설치(`BlenderFoundation.Blender`)를 제안한다.
출력에서 `[VALIDATE]` 경고, `tris`, `DONE`을 확인한다. 오류가 나면 고쳐 다시 실행한다.

## 4. 비교 → 수정 (최대 3회)
`ArtSource/Blender/previews/<Name>_34.png`, `_side.png`, `_front.png`, `_run.png`와 아이콘을 Read로 보고 원본 이미지와 비교한다.
- 실루엣·비율이 이미지와 다르면 `parts`의 `loc`·`size`·`rot`, 부품 추가·삭제, 그에 맞춰 `bones`
- 특징 부위(날개, 갈기, 뿔 등)가 이미지보다 작거나 밋밋하면 키우거나 부품을 더 겹친다
- 부위 확인: 1단계에서 적은 "있는 것"을 하나씩 미리보기와 대조한다. 부위마다 위치(이마 가운데/머리 양옆 등)·개수·크기가 이미지와 같은지 본다. 빠졌거나 원본 부위에 색만 칠해 대신했으면 새로 만든다. 미리보기에 이미지에 없는 부위(뿔, 날개, 장식 등)가 보이면 지운다
- 스타일 확인: `references/animals_overview.png`와 나란히 놓고 본다. 새 부품이 원본보다 가늘고 뾰족하거나 사실적이면 두껍고 둥글게, 개수를 줄여 단순하게
- 색이 다르면 `palette`
- 눈 위치가 어색하면 `face_kit`의 `center`·`scale`·`turn_deg`
- Run에서 다리가 몸을 뚫거나 부품이 따로 놀면 `bone` 지정
- 길게 늘어진 꼬리가 Idle_A에서 막대처럼 들리면(원본 동작이 tail.02를 크게 듦) 꼬리 tube를 `bones` 대신 `"bone": "tail.01"` 하나에 붙인다
- 눈이 안 보이면 머리 속에 묻힌 것. 머리 폭을 줄이거나 `face_kit.scale` X를 키운다(Horse 키트 폭은 배율 1에서 ±0.15m)
- 삼각형이 7,800을 넘으면 tube에 `"resolution": 4, "bevel_resolution": 2`, 깃털·장식 ellipsoid `segments`/`rings`를 10/6으로, 다리 cylinder `subdivide: 0`
사양서만 고쳐 다시 실행한다. 스크립트(`make_animal.py`, `quirky_animal_kit.py`)는 기능이 정말 부족할 때만 고치고, 고치면 FORMAT.md도 같이 고친다.

## 5. Unity 등록
`make_animal.py`가 FBX·텍스처·아이콘을 Assets에 두고 `Animals.tsv`에 행을 넣는다. **프리팹도 직접 만들어 `Assets/Resources/Animal Run/Prefabs/Runners/<Name>.prefab`에 넣는다**(사용자에게 메뉴 실행을 부탁하지 않음).
머티리얼 `M_<Name>.mat`, 컨트롤러 `AC_<Name>.controller`는 `Assets/Animal Run/Models/Runners/<Name>/`에 같이 생긴다.
1. 이 프로젝트가 Unity 에디터에 열려 있는지 확인:
   PowerShell `Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Select ProcessId, CommandLine` 에서 `-projectpath ...AnimalRun` (AssetImportWorker 줄 제외)
2. **열려 있지 않으면** 배치 모드(에디터 경로는 Unity Hub 설치 목록이나 레지스트리 Uninstall 항목에서 2023.1.17f1 확인, 이 PC는 `D:\Unity6\2023.1.17f1\Editor\Unity.exe`):
   ```
   Unity.exe -batchmode -quit -projectPath D:\Unity\GitHub\AnimalRun -executeMethod Animal_Run.Scripts.EditorTools.RunnerPrefabBuilder.BuildBatch -runner <Name> -logFile <scratchpad>\unity.log
   ```
3. **열려 있으면** 요청 파일로 열린 에디터에 빌드를 시킨다(`Scripts/Editor/RunnerPrefabRequestWatcher.cs`가 1초마다 확인).
   - 빈 파일 `Temp/RunnerPrefabRequests/<Name>.request`를 만든다. 에디터가 빌드하고 같은 폴더에 `<Name>.result`를 쓴다(마지막 줄 `RESULT OK` / `RESULT FAIL`, 빌드·검증 로그 포함).
   - 결과는 백그라운드 Bash로 기다린다: `for i in $(seq 1 180); do [ -f Temp/RunnerPrefabRequests/<Name>.result ] && break; sleep 2; done; cat ...`
   - Editor 스크립트(`RunnerPrefabBuilder.cs` 등)를 고친 직후면, 에디터가 컴파일하기 전에 옛 코드로 요청을 처리할 수 있다. 결과의 크기·기준 프리팹이 이상하면 컴파일이 끝난 뒤 한 번 더 요청한다.
   - 에디터가 Play 모드거나 컴파일 중이면 끝날 때까지 기다린다. 2~3분 안에 결과가 없으면 Unity 창을 앞으로 가져와 새로고침·컴파일을 깨운다:
     PowerShell `(New-Object -ComObject WScript.Shell).AppActivate(<에디터 ProcessId>)`. 그래도 안 되면 Play 모드인지, Console에 컴파일 오류가 있는지 사용자에게 묻는다.
4. 로그(배치는 -logFile, 에디터는 `<Name>.result`)에서 확인:
   `[RunnerPrefabBuilder] ... 기준 프리팹`, `[Verify] mesh=Mesh_LOD0 ... blendShapes=29 bones=10`(날개 동물 12), `Idle_A`/`Run` 상태, `Eyes_Blink ... bound=True`, `Resources runner=True ... sprite=True`
5. Unity 셰이더로 찍은 3/4·옆모습(에디터 요청은 `Temp/RunnerPrefabRequests/<Name>_preview.png`, 배치 모드는 `ArtSource/Blender/previews/<Name>_unity.png`)을 Read로 보고, 블렌더 미리보기와 색·모양이 같은지 확인한다.
   몸이 까맣게 나오면 정점색(Col)이 0인 것이다. 사용자에게 보고하기 전에 반드시 이 이미지로 확인한다.
   비늘 동물(사양서 `scales`)은 `shader=Animal Run/Quirky Scales`인지와 비늘 무늬도 이 이미지로 확인한다.

## 6. 보고
아이콘 이미지를 보여주고, 만든 방식(기반 동물, 바꾼 부위, 팔레트), 파일 목록, 남은 확인(플레이 테스트)을 짧게 알린다. 커밋은 사용자가 요청할 때만.

## 주의
- 메시 오브젝트 이름은 `Mesh_LOD0` (표정 anim 경로). make_animal.py가 맞춰 준다.
- 같은 `name`으로 다시 실행하면 .blend, FBX, 텍스처, 아이콘을 덮어쓴다. 기존 러너 이름(Horse, Pig 등)은 사용자가 원할 때만.
- `tools/render_references.py`는 import하면 references/ 이미지를 다시 렌더한다. 모듈로 import하지 말 것.
- Quirky 원본 에셋(`Assets/ThirdParty/...`)은 읽기만 하고 수정하지 않는다.
