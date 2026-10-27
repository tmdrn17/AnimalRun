# 인수인계: AnimalRun 블렌더 동물 제작 키트 + 시험용 돼지 러너

다른 컴퓨터에서 이어갈 때: `git fetch origin && git checkout feature/blender-animal-kit` 후
Claude Code 에 "ArtSource/Blender/HANDOFF.md 읽고 이어서 해줘" 라고 요청.

## 상황
Unity 게임 AnimalRun(Unity 2023.1.17f1, URP)에서 쓰는 러너 동물(Quirky Series Ultimate 에셋 9종)을 분석했다.
그 결과로 같은 스타일의 새 동물을 블렌더에서 만드는 키트를 만들었고, 시험용으로 돼지를 만들어 게임에 등록했다.
작업은 전부 `feature/blender-animal-kit` 브랜치에 푸시돼 있고, main에는 아직 합치지 않았다.
답변은 한국어로 해줘.

## 먼저 읽을 파일
- `ArtSource/Blender/README.md`: 원본 스타일 분석 결과, 키트 사용법, Unity 연결 방법 (가장 먼저 읽기)
- `ArtSource/Blender/quirky_animal_kit.py`: 블렌더 애드온 (N 패널 > Quirky 탭)
- `ArtSource/Blender/tools/make_test_pig.py`: 돼지를 처음부터 만들어 Pig.blend 저장, FBX·텍스처·아이콘을 Assets로 내보냄
- `ArtSource/Blender/tools/render_references.py`: 원본 동물 참고 이미지 렌더 (`references/`)
- `Assets/Animal Run/Scripts/Editor/RunnerPrefabBuilder.cs`: FBX로 러너 프리팹 생성 (메뉴 Tools > Animal Run > Build Runner Prefab From Selected FBX, 배치용 `BuildPigBatch`)

## 원본 스타일 핵심 (자세한 건 README)
- 메시 1개, 머티리얼 1개, 모든 면 Smooth. LOD0는 삼각형 3,000~7,800개. 블렌더에서 머리가 -Y 방향
- 텍스처는 16×4 팔레트 한 장. 면마다 UV를 팔레트 한 칸의 한 점에 모아 색을 칠함. Unity에서는 Point 필터
- 본 10개: root > body > ear.L/R, tail.01 > tail.02, leg.F.L/F.R/B.L/B.R (날개 동물은 wing.L/R 추가). Rig 오브젝트 스케일 0.4
- 애니메이션: 24fps, 모든 클립 11프레임, Death만 루프 안 함. 게임이 쓰는 클립은 Idle_A와 Run뿐
- 표정은 셰이프키 29개. `_Animations/Shapekey/Eyes_*.anim`이 `Mesh_LOD0` 경로의 셰이프키를 움직임

## 돼지 결과물
- 블렌더: `ArtSource/Blender/Pig.blend`, `ArtSource/Blender/textures/T_Pig.png`
- Unity: `Assets/Animal Run/Models/Runners/Pig/` (Pig.fbx, T_Pig.png, M_Pig.mat, AC_Pig.controller)
- 프리팹 `Assets/Resources/Animal Run/Prefabs/Runners/Pig.prefab`, 아이콘 `Assets/Resources/Animal Run/Sprites/Runners/Pig_Image.png`
- `Assets/StreamingAssets/Table/Animal Run/Animals.tsv`에 `9 돼지 ... Small` 행 추가
- 프리팹 콜라이더·무기 위치는 크기가 비슷한 Mousedeer 값을 그대로 씀
- Unity 배치 모드 검증 통과: 셰이프키 29개, 본 10개, 클립 18개(0.417초), 표정 애니 연결됨, Resources 로드됨

## 남은 일
1. **Unity 플레이 테스트 결과 반영 (사용자가 직접 확인 중)**: 무리 안 크기, 무기 위치, 달리기 시 다리가 몸을 뚫는지, 표정이 나오는지.
   문제가 모델이면 make_test_pig.py 값(BONES, EYE_KIT_*, 팔레트 등)을 고쳐 다시 실행하고, 프리팹이면 RunnerPrefabBuilder를 다시 돌리거나 프리팹을 고친다.
2. 브랜치를 main에 합치기 (PR 또는 직접 병합, 사용자에게 물어보기)
3. (선택) 이미 있던 버그: `PlayerAnimator.Idle()`이 `Play("Idle")`을 부르는데 컨트롤러 상태 이름은 `Idle_A`. 고칠지는 사용자에게 물어보기

## 주의할 점
- 메시 오브젝트 이름은 반드시 `Mesh_LOD0`여야 표정 애니가 동작한다
- 블렌더 `Mesh.transform()`은 셰이프키 좌표를 바꾸지 않는다. 셰이프키가 있는 메시는 키마다 좌표를 직접 변환해야 한다
- 블렌더가 내보낸 클립 이름은 `Rig|Run` 형태라서, Unity 임포트 때 `Run`으로 바꿔야 한다 (RunnerPrefabBuilder가 자동으로 처리)
- Unity 배치 모드(-executeMethod)는 이 프로젝트가 Unity 에디터에 열려 있으면 실행되지 않는다
- 이전 컴퓨터 경로: 블렌더 `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`, Unity `C:\Program Files\Unity\Hub\Editor\2023.1.17f1\Editor\Unity.exe`. 이 컴퓨터에서는 설치 위치를 먼저 확인할 것
- 스크립트 실행 예: `blender -b --factory-startup --python ArtSource/Blender/tools/make_test_pig.py` (저장소 루트에서 실행)
