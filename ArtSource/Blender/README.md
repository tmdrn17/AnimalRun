# Quirky 스타일 동물 제작 가이드 (Blender)

게임에 쓰이는 러너 동물 9종(`Assets/ThirdParty/Quirky Series Ultimate`)을 블렌더 5.2로 직접 열어 분석한 결과와,
같은 규격으로 새 동물을 만드는 도구(`quirky_animal_kit.py`) 사용법입니다.

| 파일 | 내용 |
|---|---|
| `quirky_animal_kit.py` | 블렌더 애드온 (N 패널 > **Quirky** 탭) |
| `references/animals_overview.png` | 9종 3/4뷰 + 측면 |
| `references/Horse_*_strip.png` | 말 Run/Idle_A/Walk/Jump/Attack/Death 11프레임 |
| `references/palettes.png` | 9종 팔레트 텍스처 (위에서부터 Horse, Zebra, Donkey, Camel, Llama, Wildebeest, Mousedeer, Kirin, Pegasus) |
| `tools/render_references.py` | 위 이미지 재생성 스크립트 |

---

## 1. 분석 결과 — 스타일 규칙

### 모델
| 항목 | 원본 값 |
|---|---|
| 구성 | 메시 1개 + 머티리얼 1개 + UV 1개, 전부 Smooth 셰이딩 |
| 폴리곤 | LOD0(게임에서 사용) 삼각형 3,000~7,800 / 애니메이션 FBX용 저폴리 1,500~3,000 |
| 크기(블렌더, m) | 길이(Y) 1.1~2.1, 높이(Z) 0.74~1.56, 폭(X) 0.43~0.88 |
| 방향 | 머리가 **-Y**(블렌더 Front 뷰를 바라봄), Z 위, 발바닥 Z=0 |
| 형태 | 큰 원통형 몸통 + 짧은 목/머리가 한 덩어리, 가늘고 곧은 막대 다리(끝 1~2cm는 발굽 색), 큰 원형 눈(흰자+검은 눈동자+하이라이트), 작은 귀, 2마디 꼬리 |
| 버텍스 컬러 | `Col` 레이어가 있지만 거의 흰색(사용 안 함) — 만들 필요 없음 |

### 텍스처 — 16×4 팔레트 아틀라스
- 모든 동물이 **16×4 px PNG 한 장**. 2px 폭 슬롯 8개 × (위 2줄 / 아래 2줄).
- UV는 **면 단위로 한 점에 모여** 있습니다(면 안 UV 폭 ≈ 0). 즉 "면을 팔레트 칸에 찍어서 색칠"하는 방식이고, 명암은 전부 조명 + 스무스 노멀로 냅니다.
- Unity 임포트: Filter **Point**, 머티리얼은 URP `Particles/Lit` 셰이더(M_Horse 복제해서 텍스처만 교체).

| 슬롯 | 픽셀 X | 위(줄 3·4) | 아래(줄 1·2) |
|---|---|---|---|
| 1~5 | 0~9 | 동물 고유 몸 색 5개 (밝은→어두운, 주둥이/배/갈기/다리/발굽 등) | 보통 위와 동일 (Mousedeer처럼 변형색 가능) |
| 6 | 10~11 | 포인트 색(갈기·코 등) | 입속 핑크 `#cc7d95` |
| 7 | 12~13 | 눈 흰자 `#f9f8f2` | 땀 노랑 `#fff263` |
| 8 | 14~15 | 눈동자 `#292939` | 눈물 파랑 `#63cdff` |

슬롯 6~8의 공용 색(핑크·흰자·눈동자·노랑·파랑)은 **9종 모두 동일**하니 새 동물도 그대로 두세요.

### 리그
본 10개(날개 동물 12개), 이름·계층이 전 동물 공통:
```
Rig (오브젝트 스케일 0.4, Kirin/Pegasus 0.56)
└ root
  └ body          ← 머리/몸통 전체. Weapon Position 이 여기 붙음
    ├ ear.L / ear.R
    ├ tail.01 └ tail.02
    ├ leg.F.L / leg.F.R / leg.B.L / leg.B.R   (다리 하나 = 본 하나, 무릎 없음)
    └ (wing.L / wing.R)
```
- 목·머리·턱 본이 없습니다. 머리 움직임은 `body` 회전 + 스케일로 표현.
- 가중치는 거의 강체(파트별 100%)에 이음매만 부드럽게.

### 애니메이션
- **24fps, 모든 클립 11프레임(0~10)**, Death만 비루프. 리니어 키 매 프레임.
- 클립 18개: Attack, Bounce, Clicked, Death, Eat, Fear, Fly, Hit, Idle_A/B/C, Jump, Roll, Run, Sit, Spin, Swim, Walk
- **게임 AnimatorController가 쓰는 건 `Idle_A`, `Run` 두 개뿐**입니다(나머지는 선택).
- 스타일 핵심: 짧고 빠른 루프 + **본 스케일로 스쿼시&스트레치**
  - Idle_A: body 위아래 위치 ±0.2 + Y스케일 0.95, 다리 스케일 0.86~1.12, 귀·꼬리 흔들기
  - Run: 다리 앞뒤 회전 약 ±25°(앞다리 쌍/뒷다리 쌍이 같이 움직이는 깡총 뛰기), body 피치 ±7°, 귀·꼬리 따라오기
  - Jump: body/다리 Y스케일 0.8까지 눌렀다 펴기
- 표정은 셰이프키 29개(`eyes.blink`, `eyes.happy` … `sweat-2.R`)로 하고,
  `_Animations/Shapekey/*.anim`이 `blendShape.<이름>`으로 이 키를 움직입니다. **이름이 같으면 새 동물에서도 그대로 재사용** 가능.

---

## 2. 애드온 설치

1. Blender > Edit > Preferences > Add-ons > 오른쪽 위 `v` > **Install from Disk…** > `quirky_animal_kit.py`
2. 3D 뷰에서 `N` > **Quirky** 탭
3. 블렌드 파일을 이 저장소 안(예: `ArtSource/Blender/MyAnimal.blend`)에 저장하면 Quirky 폴더를 자동으로 찾습니다. 못 찾으면 `Quirky 폴더`에 직접 지정.

## 3. 작업 순서

1. **새 파일 → 저장** (`ArtSource/Blender/<이름>.blend`)
2. **참조 동물** 선택(체형이 가장 비슷한 것) → **참조 리그로 새 동물 시작**
   - `Rig` + 18개 액션(이름 `Run`, `Idle_A` …, 1~11프레임, 루프 설정)을 가져옵니다.
   - 참조 메시는 `REF_…`(와이어, `QAK_Reference` 컬렉션)로 남아 같이 애니메이션되니 비율 비교에 쓰고, 다 끝나면 지워도 됩니다.
3. **모델링**: 새 메시를 만들고(머리 -Y, 발 Z=0, 크기는 REF 와 비슷하게) → Ctrl+A 스케일 적용 → Shade Smooth
   - 메시 오브젝트 이름은 반드시 **`Mesh_LOD0`** (표정 anim `Eyes_*`가 이 경로의 블렌드셰이프를 움직임)
   - 필요하면 Rig 편집 모드에서 본 위치를 새 체형에 맞게 이동(이름·계층·대략적 방향은 유지)
   - 메시 선택 → Shift+Rig 선택 → Ctrl+P **With Automatic Weights** → 다리/귀/꼬리 가중치 정리
4. **팔레트**: `참조 팔레트 불러오기`로 시작 색을 가져와 슬롯 1~6 위 색을 수정 → `팔레트 텍스처/머티리얼 생성`
   (`textures/T_<이름>.png`, 머티리얼 `M_<이름>`)
   - 편집 모드에서 면 선택 → 슬롯 옆 🖌 버튼으로 칠하기, 👆 버튼으로 그 색 면 선택
   - 뷰포트 Solid 모드 Color를 **Texture**로 두면 바로 보입니다.
5. **눈/표정**: `표정 키트 가져오기` → `FaceKit_…`(눈·눈물·땀 + 셰이프키 29개)를 새 머리 위치로 이동/회전/크기 조절
   → FaceKit 선택 후 본체를 Shift 클릭(활성) → `표정 키트를 본체에 합치기`
   - 눈 가중치는 `body` 그룹으로 들어옵니다. 합친 뒤 셰이프키 몇 개 켜서 확인.
6. **애니메이션**: Dope Sheet > Action Editor에서 액션을 골라 수정. 참조 동물 동작이 이미 들어 있으니
   새 비율에 맞게 다리 각도·몸 높이만 다듬으면 됩니다. 최소 `Idle_A`, `Run`은 꼭 확인.
   - 11프레임(1~11), 첫/마지막 프레임 같은 포즈로 루프 유지
7. **규격 검사** → 경고 확인
8. **Unity용 FBX 내보내기** (`내보낼 폴더`/`<이름>.fbx`). Rig + 메시 + 셰이프키 + 모든 액션이 한 파일로 나가며 `REF_`, `FaceKit_`은 제외됩니다.
   - 축(Y-up, Z-front), 단위, 24fps, 0~10프레임, 본 이름이 원본 FBX와 같게 나오는 것을 확인했습니다.

## 4. Unity 연결

### 자동 (권장)
1. `Assets/Animal Run/Models/Runners/<Name>/`에 `<Name>.fbx`, `T_<Name>.png`를 두고,
   아이콘은 `Assets/Resources/Animal Run/Sprites/Runners/<Name>_Image.png` (256px 투명)
2. Project 창에서 FBX 선택 → 메뉴 **Tools > Animal Run > Build Runner Prefab From Selected FBX**
   (`Assets/Animal Run/Scripts/Editor/RunnerPrefabBuilder.cs`) — 아래 수동 과정을 그대로 해 줍니다.
   콜라이더·무기 위치는 Mousedeer 값을 쓰므로 큰 동물이면 프리팹에서 조정하세요.
3. `Assets/StreamingAssets/Table/Animal Run/Animals.tsv`에 행 추가 (ID, 이름, 프리팹 키, 스프라이트 키, Small/Medium/Large)

예시: `tools/make_test_pig.py`가 돼지를 처음부터 만들어 `Pig.blend`, FBX, 텍스처, 아이콘까지 내보냅니다.

### 수동
1. FBX를 `Assets/Animal Run/Models/Animals/` 등에 넣고, 텍스처는 Filter Mode **Point**.
2. FBX Rig 탭: Animation Type **Generic**. Animation 탭: 클립 이름이 `Rig|Run`처럼 나오니 `Run`, `Idle_A`로 이름 바꾸기(Loop Time 체크, Death 제외).
3. 머티리얼: `M_Horse` 복제 → `_BaseMap`만 새 텍스처로 교체.
4. AnimatorController: `AC_Horse` 복제 → `Idle_A`, `Run` 상태의 Motion을 새 클립으로 교체
   (셰이프키 이름이 같으면 표정 레이어 `Eyes_*` 클립은 그대로 동작).
5. 프리팹: `Resources/Animal Run/Prefabs/Runners/Horse.prefab` 복제 → 모델 교체 →
   `Weapon Position`을 `Rig/root/body` 아래로 다시 연결, `Runner` 컴포넌트의 weaponPosition 지정,
   CapsuleCollider(반지름 0.6, 높이 1.46) 크기 확인. 루트 스케일은 기존과 같은 1.5.
6. 선택 UI 아이콘: `Resources/Animal Run/Sprites/Runners/<이름>_Image.png`

> 참고: `PlayerAnimator.Idle()`은 `Play("Idle")`을 호출하는데 컨트롤러의 상태 이름은 `Idle_A`입니다.
> 기존 동물에도 해당되는 부분이라 이번엔 건드리지 않았습니다.
