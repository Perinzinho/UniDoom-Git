# Prison third-party assets

Source: Poly Haven. All eight assets are published under CC0 1.0.
License: https://polyhaven.com/license
CC0 legal text: https://creativecommons.org/publicdomain/zero/1.0/legalcode
Retrieved: 2026-10-20.

| Asset | Author | Source | Use |
| --- | --- | --- | --- |
| Old Bed Frame | Luca B | https://polyhaven.com/a/old_bed_frame | Six rusted metal beds |
| Vintage Day Bed | Aron Łyczek | https://polyhaven.com/a/vintage_day_bed | Extracted mattress and pillow geometry; original atlas retained |
| Large Iron Gate | Josh Dean | https://polyhaven.com/a/large_iron_gate | Cell door leaves, fitted to the existing frontages |
| Caged Hanging Light | Ulan Cabanilla | https://polyhaven.com/a/caged_hanging_light | Four ceiling luminaires and extracted restraint chain sections |
| Gate Latch 01 | Desktoy | https://polyhaven.com/a/gate_latch_01 | Cell latches and wall restraint anchors |
| Metal Stool 01 | Ulan Cabanilla | https://polyhaven.com/a/metal_stool_01 | Cell stools |
| Wooden Bucket 01 | James Ray Cock | https://polyhaven.com/a/wooden_bucket_01 | Custody buckets |
| Wooden Bowl 01 | Oliver Harries | https://polyhaven.com/a/wooden_bowl_01 | Prisoner bowls |

Source FBX files, albedo maps and OpenGL normal maps were downloaded from the official API and verified against its MD5 checksums. Bed, gate and bedding maps use 2K; smaller props use 1K. Materials were adapted to Unity 6 URP Lit. Source AO/Roughness/Metal maps were repacked as Mask.png: R = metallic, G = ambient occlusion, B = unused, A = 1 - roughness. Source UVs were retained when extracting parts. The imported meshes have simple gameplay colliders; the hidden passage and boss event remain part of Basement.prefab.