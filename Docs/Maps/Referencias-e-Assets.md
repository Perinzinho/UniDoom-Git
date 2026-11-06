# Referências e assets gratuitos — consulta sob demanda

Verificado em **2026-11-05**. Não leia este catálogo para uma mudança que não envolve arte/assets. Prefira o inventário existente; pesquise somente o item ausente. Confirme a página e a licença do arquivo escolhido na data de um novo download.

## Direção visual e arquitetura usadas

| Referência | Uso | Limite |
|---|---|---|
| `.codex/reference/direito reference.png` | Organização térrea, hall, quatro corredores, salas, sanitários e identidade do bloco | Imagem enviada pelo usuário; não é planta cotada nem fonte de modelos licenciados |
| [Total Chaos clássico / ModDB](https://www.moddb.com/mods/total-chaos), por **wadaholic** no portal | Materiais ásperos, abandono causal, contraste e enquadramento | Referência visual; não extrair/reutilizar assets do mod |
| [Hotel Lobby](https://www.moddb.com/mods/total-chaos/images/hotel-lobby) e [Shader implementation](https://www.moddb.com/mods/total-chaos/images/shader-implementation) | Comparação de iluminação, superfícies e profundidade | Observar imagens reais; não inferir implementação interna do mod |
| [Unimar — institucional](https://oficial.unimar.br/universidade/) | Contexto de campus e leitura das ruas externas | Fotografias não equivalem a licença de textura |
| [Univem — notícia com vista do campus](https://www.univem.edu.br/noticias/univem-lanca-seis-novos-cursos-tecnologos-na-area-de-gestao-e-negocios-para-o-vestibular-2026) | Ruas, calçadas e implantação externa | O usuário corrigiu “Unidoom” para **UNIVEM** |
| [Google Maps — Unimar, Marília](https://www.google.com/maps/search/?api=1&query=UNIMAR+Marilia) / [Univem, Marília](https://www.google.com/maps/search/?api=1&query=UNIVEM+Marilia) | Referência espacial das vias na revisão anterior | Não importar imagens de Maps como texturas do jogo |

A referência artística normativa do projeto é `.codex/SKILL.md`; as fotos reais orientam função/escala, e a skill orienta tratamento de abandono e luz. Não uniformize todos os ambientes com ferrugem ou névoa.

## Assets efetivamente usados e autores

| Asset/fonte primária | Autor confirmado | Licença e aplicação |
|---|---|---|
| [School Chair 01](https://polyhaven.com/a/SchoolChair_01) | **Ethan Place**; [metadados oficiais](https://api.polyhaven.com/info/SchoolChair_01) | CC0; cadeira escolar, altura normalizada 0,88 m |
| [School Desk 01](https://polyhaven.com/a/SchoolDesk_01) | **Ethan Place**; [metadados oficiais](https://api.polyhaven.com/info/SchoolDesk_01) | CC0; carteira, altura 0,76 m |
| [Toilets](https://opengameart.org/node/165996) | **loafbrr_1** / [loafbrr](https://loafbrr.itch.io/) | CC0; selecionados `Toilet_Elongated_A`, `Sink_A`, `Urinal_A`, texturas 1K |
| [Power Box 01](https://polyhaven.com/a/power_box_01), [Metal Toolbox](https://polyhaven.com/a/metal_toolbox), [Tool Cart](https://polyhaven.com/a/tool_cart), [Flathead Screwdriver](https://polyhaven.com/a/flathead_screwdriver), [Wooden Hammer 01](https://polyhaven.com/a/wooden_hammer_01) | **Rico Cilliers/Yann Kervran; Mateusz Sadek; Savva Zakharov; Dylan Detwiller; James Ray Cock**, respectivamente, conforme API oficial | CC0; manutenção 104 de LawSchool; FBX/PBR 1K; hashes e conversões em `Licenses/AssetSources/LawSchool/maintenance-sources.json` |
| [Clean Asphalt](https://polyhaven.com/a/clean_asphalt) | **Dimitrios Savva**; [metadados oficiais](https://api.polyhaven.com/info/clean_asphalt) | CC0; pavimento de CampusExterior, não do interior LawSchool |

Proveniência durável: `Licenses/AssetSources/LawSchool/{sources.json,sanitary-sources.json,sanitary-readme.txt}`; para as ruas, `Licenses/AssetSources/CampusExterior/road-revision-sources.json`. Esses arquivos registram URLs, SHA256 e conversões. Texturas compartilhadas anteriores foram reutilizadas; consulte seus manifestos antes de atribuir novos autores. Não invente autoria individual ausente.

## Sites para pesquisas futuras

| Prioridade | Fonte e autoria | Uso recomendado | Verificação |
|---|---|---|---|
| 1 | [Poly Haven](https://polyhaven.com/models) — equipe e artistas identificados por asset | Mobiliário com aparência realista, modelos escaneados, HDRIs e PBR | [Licença CC0](https://polyhaven.com/license); registrar autor do item, não atribuir tudo ao mesmo artista |
| 1 | [ambientCG](https://ambientcg.com/) | Reboco, concreto, ladrilhos, pavimento e materiais PBR | [Licença oficial CC0](https://docs.ambientcg.com/license/); escolher escala física e resolução adequadas |
| 2 | [OpenGameArt](https://opengameart.org/) — autores por página | Props específicos ausentes do inventário; exemplo já usado: loafbrr_1 | Licenças variam por item; verificar página, arquivo incluído e autoria |
| 3 | [Quaternius](https://quaternius.com/) — Quaternius | Módulos/props quando a silhueta e a geometria combinarem com o jogo | [FAQ/licença](https://quaternius.com/faq.html); modelos CC0; conferir o pacote, pois vários têm acabamento estilizado |
| 3 | [Kenney](https://kenney.nl/assets) — Kenney | Bloqueio e props discretos compatíveis, adaptando material quando necessário | [Suporte/licença CC0 dos assets](https://kenney.nl/support); conferir licença incluída; não usar a marca/logo do estúdio |

Kenney e Quaternius são opções de pesquisa, **não indicação de que seus modelos foram usados neste mapa**. Compatibilidade visual prevalece sobre disponibilidade. A skill pede realismo áspero, não transformação do cenário inteiro em low-poly estilizado.

## Seleção e importação com pouco contexto

1. Identifique a peça, dimensão e enquadramento necessários; busque primeiro nos prefabs/meshes/materiais existentes. Pesquise 2–3 candidatos focados, sem listar catálogos inteiros.
2. Abra fonte primária, preview, autor e licença do download. Prefira CC0; outras licenças exigem conferir uso e redistribuição com o jogo e cumprir atribuição. Ignore opções com custo, permissão incerta ou dependência incompatível; não compre nem crie contas sem autorização.
3. Prefira FBX/OBJ/glTF importável pela configuração existente. Não instale conversores/plugins para contornar um único asset sem necessidade. Baixe o pacote em temporário e copie somente modelos/texturas realmente selecionados.
4. Normalize por bounds/vértices reais, confira metros, pivô, contato, materiais, normais e caixa de colisão. Use PBR e textura 1K para props repetidos como ponto de partida; confirme a aparência antes de mudar resolução.
5. Confirme canais da máscara e GL/DX do normal map. Guarde SHA256 antes e depois de conversão. Gere meshes/prefabs/materiais persistentes sob a pasta da cena; compartilhados vão para Global conforme AGENTS.md.
6. Registre `arquivo, source, download, autor, licença, SHA256, data` no manifesto do cenário; acrescente `source_sha256` e descrição da conversão quando o arquivo mudar. Não guarde pacote inteiro ou ferramentas desnecessárias em Assets.
7. Compare dentro do jogo e teste colisão/culling. Depois, retire somente temporários desta tarefa após verificar dependências, código e carregamento dinâmico.

Consultas focadas úteis: `university classroom chair CC0 FBX`, `institutional sink urinal CC0 PBR`, `aged plaster CC0 texture`. Leia as condições atuais da fonte; este catálogo não substitui a licença do asset escolhido.
