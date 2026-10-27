Hell Map Tiles — 256x256 개별 PNG (Forest Style B / Ice Map과 같은 구조)

[Unity 설정 - 전부 선택 후 Apply]
Sprite Mode: Single / Pixels Per Unit: 100 / Mesh Type: Tight
Filter Mode: Point (no filter) / Compression: None
Props는 Pivot: Bottom
Grid Cell Size: (2.56, 2.56, 0)

[Tiles]  ※ 파일명은 숲/얼음 버전과 동일 → Rule Tile 복제 후 스프라이트만 교체
Grass/grass_0~3   그을린 화산암 바닥 4종 (불씨 점 포함)
Path/path_NESW    잿빛 돌길 16종 (위/오른쪽/아래/왼쪽 연결 1=연결)
Water/water_NESW  용암 강 16종 (굳은 용암 껍질 + 빛나는 가장자리)

[Props]
tree_burning_0/1, tree_burning_small, pine_charred_0/1, dead_tree_0/1, bush_0, bush_small
rocks_cluster, rock, stone_wall_long/short, stump, log, bridge_h, bridge_v
ash_patch, lava_pool, lava_vent, lava_crystal, lava_crystal_small, brazier(화로)
skull, bones, embers(불씨), burnt_grass

_32px 폴더: 원본 32px 버전
팁: URP 2D 사용 시 용암/화로/크리스탈에 주황색 Point Light 2D를 붙이고 Global Light를 0.5 정도로 낮추면 분위기가 확 살아남
