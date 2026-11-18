# Passos de grama e pavimento

- Autor: **Kenney (Kenney Vleugels)**, pacote **Impact Sounds 1.0**.
- Fonte oficial e licença declarada CC0: https://kenney.nl/assets/impact-sounds
- Termos CC0 1.0: https://creativecommons.org/publicdomain/zero/1.0/
- Download oficial retornou HTTP 403; usados os mesmos nomes de clips no espelho público `iree-gd/iree.gd`, commit `9db7d7069a9ad75c77e9b5657d1ae5bdf615a6bc`. Nenhum código do espelho foi incorporado.
- `sources.json` registra URLs exatas, data, SHA256 e opções de importação de cada arquivo. Cada download foi conferido contra o SHA de blob informado pela API do GitHub.
- Somente dez OGG necessários foram importados, sem alteração dos bytes. A Unity os importa em mono, com pré-carregamento e descompressão ao carregar.
- **Restaurados em 18/11/2026:** apenas `footstep_grass_000.ogg` e `footstep_grass_002.ogg`, a pedido do usuário (máximo de dois clipes de grama). Os bytes conferem com o SHA-256 registrado em `sources.json`. Os cinco de pavimento e os outros três de grama continuam ausentes do working tree; o banco de pavimento cai para concreto até nova decisão. Os dois clipes restaurados foram reimportados em mono, pré-carregados e com descompressão ao carregar.
- `footstep_grass_*`: grama. `footstep_concrete_*`: passos secos em superfície dura, usados para representar o asfalto. O autor não rotula essas gravações como asfalto. Os bancos anteriores continuam atendendo concreto, madeira e interiores.
