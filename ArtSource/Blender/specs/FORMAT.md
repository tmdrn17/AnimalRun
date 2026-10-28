# 동물 사양서 형식 (`specs/<Name>.json`)

`tools/make_animal.py`가 이 JSON 하나로 블렌더 모델, Unity FBX, 텍스처, 아이콘을 만들고 `Animals.tsv`에 행을 추가합니다.

```
blender -b --factory-startup --python ArtSource/Blender/tools/make_animal.py -- ArtSource/Blender/specs/<Name>.json
```

결과물
- `ArtSource/Blender/<Name>.blend`, `ArtSource/Blender/textures/T_<Name>.png`
- `Assets/Animal Run/Models/Runners/<Name>/<Name>.fbx`, `T_<Name>.png`
- `Assets/Resources/Animal Run/Sprites/Runners/<Name>_Image.png` (256px 아이콘)
- `ArtSource/Blender/previews/<Name>_*.png` (비교용 렌더: 3/4, 옆, 앞, Run 중간 프레임. git 제외)
- `Animals.tsv`에 같은 프리팹 키가 없으면 행 추가

같은 이름으로 다시 실행하면 위 파일을 모두 덮어씁니다. 프리팹은 Unity에서 범용 빌더로 만듭니다(README 4장).

## 좌표 규칙
- 단위 m, 블렌더 월드 좌표. **머리가 -Y**, 위가 +Z, 발바닥 Z=0, 좌우 대칭 축 X=0 (L이 +X).
- 회전 `rot`은 도(°) 단위 XYZ 오일러.
- 팔레트 슬롯은 **1~8**, `half`는 `"top"`(위 2줄) / `"bottom"`(아래 2줄). README의 슬롯 표와 같습니다.
  6아래(입속), 7(눈 흰자/땀), 8(눈동자/눈물)은 공용색이라 바꾸지 않습니다.

## 최상위 필드

| 필드 | 필수 | 설명 |
|---|---|---|
| `name` | O | 영문 이름. 파일·프리팹 이름 (`Unicorn`) |
| `display_name` | O | 게임 표시 이름 (`유니콘`) |
| `kind` | | `Small` / `Medium` / `Large` (Animals.tsv). 기본 `Medium` |
| `notes` | | 이미지 분석 메모 등 자유 텍스트 (스크립트는 무시) |
| `base` | O | 시작점. 아래 참고 |
| `palette` | | 팔레트 덮어쓰기 |
| `alpha` | | 팔레트 칸별 불투명도 (반투명 몸) |
| `scales` | | 비늘 셰이더 (몸에 반원 비늘 명암) |
| `bones` | | 본 위치 (procedural 모드에서 필수) |
| `parts` | | 도형 부품 목록 |
| `face_kit` | | 눈/표정 키트 배치 (procedural 모드에서 필수) |
| `size` | | 전체 크기 배율 (리그 스케일에 곱함). 기본 1 |

### `base`
```json
"base": {
  "reference": "Pegasus",
  "mode": "reference_mesh",
  "remove_parts": ["wing.L", "wing.R"],
  "part_scale": {"ear.L": [1.3, 1.3, 1.3], "ear.R": [1.3, 1.3, 1.3]},
  "repaint": [{"from": [5, "top"], "to": [4, "top"], "group": "tail.01"}],
  "target_tris": 3600,
  "voxel": 0.012
}
```
- `reference`: 리그와 18개 애니메이션, 시작 팔레트를 가져올 원본. Horse, Zebra, Donkey, Camel, Llama, Wildebeest, Mousedeer, Kirin, Pegasus
- `mode`
  - `"reference_mesh"`: 원본 LOD0 메시(셰이프키 29개 포함)를 그대로 쓰고 부위를 빼거나 키우고 색을 바꿉니다. 체형이 원본과 비슷하면 이쪽이 품질이 가장 좋습니다. Donkey는 LOD0가 없어 쓸 수 없습니다.
  - `"procedural"`: `parts`의 도형으로 몸을 새로 만듭니다(돼지 방식). 이때 `bones`, `face_kit`이 필요합니다.
- `remove_parts` (reference_mesh): 이 본이 주 가중치인 버텍스를 삭제. `wing.L/R`은 본과 애니메이션 커브도 지웁니다. 표준 본은 본을 남깁니다.
- `remove_mesh` (reference_mesh): `remove_parts`와 같이 버텍스를 지우되 **본과 애니메이션은 남깁니다**. 원본 날개를 지우고 새 날개 부품(`bone: "wing.L"`)을 붙일 때.
- `remove_slots` (reference_mesh): `[[5, "bottom"]]`처럼 이 팔레트 칸을 쓰는 면을 삭제. 본이 따로 없는 부위(페가수스 뿔 = 5아래)를 뗄 때.
- `part_scale` (reference_mesh): 이 본이 주 가중치인 버텍스를 본 머리 기준으로 XYZ 배율 조절(셰이프키 포함). 다리는 길이를 바꾸면 발이 땅에서 뜨거나 묻히니 굵기(X·Y)만 바꾸세요.
- `repaint` (reference_mesh): 팔레트 칸 `from`을 쓰는 면을 `to`로 옮깁니다. `group`을 주면 그 본이 주 가중치인 면만.
  `z_min`/`z_max`(월드 Z, m)를 주면 면 중심 높이가 그 범위인 면만 (같은 칸·같은 본인 얼굴과 뿔을 나눌 때. 예: Kirin 뿔은 1위 body 칸 중 Z 1.3 이상, 뿔대는 2위 body 칸 중 Z 1.28 이상).
  `"delete": true`면 칸을 옮기지 않고 그 면을 지웁니다(`to` 생략). 본·칸으로 못 가르는 원본 부위를 뗄 때.
- `target_tris`, `voxel` (procedural): 몸통 덩어리 리메시 크기와 목표 삼각형 수. 기본 0.012, 3600.

원본별 팔레트 칸 쓰임은 `references/palettes.png`와 README를 보세요. 예: Pegasus는 1 몸, 2 다리·귀 안쪽, 3 발굽, 4 갈기, 5 갈기 그림자·날개, 5아래 뿔.

### `palette`
```json
"palette": {
  "top":    ["#ffd84f", "#f5b731", "#8a5a2b", "#ff8fc4", "#c27ddc", null, null, null],
  "bottom": [null, null, null, null, "#fff4dc", null, null, null]
}
```
8칸 배열, `null`은 원본(`base.reference`) 색 유지. 배열을 생략해도 됩니다.

### `alpha`
```json
"alpha": {
  "top":    [0.55, 0.55, 0.55, 0.6, 0.6, 0.65, null, null],
  "bottom": [0.55, 0.55, 0.55, 0.6, 0.6, null, null, null]
}
```
`palette`와 같은 8칸 배열, 값은 0~1 불투명도, `null`은 1(불투명). 유령처럼 몸이 비치는 동물용.
팔레트 PNG에 알파가 들어가고, Unity `RunnerPrefabBuilder`는 텍스처에 알파가 있으면 머티리얼을 Transparent(ZWrite 켬)로 만들고,
깊이만 먼저 그리는 `M_RunnerDepthPrepass`(셰이더 `Animal Run/Runner Depth Prepass`)를 두 번째 머티리얼로 붙입니다(머리 속 표정 메시·반대쪽 눈동자가 비치지 않게).
같은 메시라 몸 뒤쪽 다리·꼬리는 비치지 않고 배경만 비칩니다. 눈(7·8)과 입속(6아래)은 `null`로 두세요. 예: `specs/GhostHorse.json`

### `scales`
```json
"scales": {"density": 12, "strength": 0.4, "slots": [1]}
```
- `density`: 1m당 비늘 개수(기본 16), `strength`: 비늘 테두리 어둡기 0~1(기본 0.35), `slots`: 비늘을 넣을 팔레트 칸(1~8, 위·아래 칸 모두)
- 기본 자세의 위치·노멀을 UV2~4에 굽고 `Assets/Animal Run/Models/Runners/<Name>/<Name>.scales.json`을 씁니다.
  `RunnerPrefabBuilder`는 이 파일이 있으면 머티리얼을 `Animal Run/Quirky Scales`(Particles/Lit 와 같은 조명·정점색 + 비늘)로, 없으면 원래 셰이더로 둡니다.
  무늬가 기본 자세 좌표에 붙어 있어 뛰어도 비늘이 피부 위를 미끄러지지 않습니다.
- 블렌더 미리보기에는 비늘이 안 보입니다. Unity 빌드 때 남는 `Temp/RunnerPrefabRequests/<Name>_preview.png`로 확인하세요. 예: `specs/SeahorseSteed.json`

### `bones` (procedural 필수, reference_mesh 선택)
본 이름 → `[[head x,y,z], [tail x,y,z]]` 월드 좌표. 계층과 이름은 원본 리그 그대로 두고 위치만 옮깁니다.
reference_mesh에서는 `remove_mesh`로 메시를 지운 부위의 본만 옮기세요(남은 원본 메시가 붙은 본을 옮기면 애니메이션 회전 중심이 어긋납니다).
`tail.02`는 `tail.01` 끝에 연결됩니다. 다리 본은 위(엉덩이)에서 아래(발)로.
```json
"bones": {"body": [[0, 0, 0.42], [0, 0, 0.82]], "leg.F.L": [[0.15, -0.17, 0.34], [0.15, -0.17, 0.0]]}
```

### `parts`
| 필드 | 설명 |
|---|---|
| `name` | 부품 이름 (로그용) |
| `shape` | `ellipsoid`, `cylinder`, `cone`, `tube` |
| `loc`, `rot` | 중심 위치, 회전(도). `tube`는 무시 |
| `size` | ellipsoid `[반지름 x, y, z]` / cylinder `[반지름 x, 반지름 y, 길이]` / cone `[밑면 반지름 x, y, 길이]` (축은 로컬 Z) |
| `tip` | cone 끝 반지름 비율(0~1). 기본 0.05 |
| `points`, `radius`, `taper` | tube 경로 점들(월드), 굵기, 끝 굵기 비율(기본 1). 꼬리·휘어진 뿔·갈기 가닥용 |
| `resolution`, `bevel_resolution` | tube 경로 분할(기본 8)과 단면 둥글기(기본 3). 가닥이 많아 삼각형이 넘치면 4 / 2 로 |
| `segments`, `rings` | 분할 수 |
| `bone` | 이 부품이 따라 움직일 본. 기본 `body` |
| `bones` | tube 전용 `[시작 본, 끝 본]`. 경로를 따라 가중치가 넘어감 (꼬리 `["tail.01", "tail.02"]`) |
| `slot`, `half` | 칠할 팔레트 칸. 기본 1, top |
| `tip_slot`, `tip_half`, `tip_height` | 면 중심 Z가 `tip_height` 아래면 이 칸 (다리 발굽) |
| `merge` | procedural 모드에서 true면 몸통 덩어리에 리메시로 녹여 합침(몸·머리·주둥이·귀). 기본: procedural은 true, reference_mesh는 항상 false |
| `bevel`, `subdivide` | 따로 붙는 부품의 모서리 둥글리기. 기본 cylinder 0.02 / 1, 그 외 0 / 0 |

`merge` 부품의 버텍스는 가장 가까운 merge 부품의 본·색을 받습니다(부품 크기로 정규화한 거리 기준).

### `face_kit` (procedural)
원본 LOD0의 눈·눈물·땀(셰이프키 29개)을 떼어 새 머리에 붙입니다.
```json
"face_kit": {"reference": "Horse", "center": [0, -0.44, 0.615], "scale": [0.78, 0.9, 0.9], "turn_deg": 40, "push": 0.012}
```
- `center`: 두 눈 묶음의 중심 위치, `scale`: XYZ 배율
- `turn_deg`: 옆을 보는 눈을 앞(-Y)으로 돌리는 각도, `push`: 돌린 뒤 앞으로 미는 거리

## 예시
- `examples/Unicorn.json`: reference_mesh (페가수스에서 날개 제거 + 색 변경)
- `examples/Pig.json`: procedural (`tools/make_test_pig.py`를 사양서로 옮긴 것. 귀 가중치·귀 안쪽 색 같은 세부는 단순화)
