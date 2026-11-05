# Organização dos assets Unity

- Mantenha somente o conteúdo necessário ao jogo e ao projeto Unity. A pedido do usuário, backups, quarentenas, geradores e auditorias anteriores foram excluídos; não recrie essas pastas sem solicitação.
- Cada cenário novo deve ter pasta própria em `Assets/_Project/Scenes/<Nome>/`. Salve nela todos os assets exclusivos e crie subpastas por tipo somente quando houver conteúdo.
- Recursos compartilhados entre cenários ou sistemas ficam em `Assets/_Project/Global/`, separados por tipo. Verifique o inventário e reutilize materiais, texturas e meshes existentes antes de gerar cópias.
- Preserve em `Assets/_Project/Editor/` apenas integrações necessárias aos assets, como o inspector `TotalChaosMaterialGUI` dos shaders usados. Scripts de gameplay e ajustes de layout necessários continuam em seus cenários ou sistemas globais.
- Para gerações pontuais, prefira ferramentas fora de Assets; quando precisar compilar código temporário no Editor, mantenha-o numa área de ferramentas temporárias identificada. Salve objetos, materiais, meshes e outros resultados em assets persistentes.
- Só arquive um gerador após verificar os resultados salvos, dependências por GUID e código, carregamento dinâmico e necessidade de regeneração. Preserve ferramentas ainda necessárias.
- Nunca considere um asset descartável apenas porque não está referenciado por uma cena. Verifique todas as cenas, prefabs, ScriptableObjects, configurações, Resources, Addressables, AssetBundles, shaders/includes e caminhos construídos em código.
- Preserve GUIDs, fileIDs e importadores. Prefira `AssetDatabase.MoveAsset`. Nunca mova um asset sem seu `.meta`; mantenha pastas especiais e pacotes externos quando sua localização for relevante.
- Atualize caminhos de texto nos consumidores ativos ao mover assets. Não regenere as antigas árvores `Assets/Art`, `Assets/prefabs` ou `Assets/TotalChaos` inadvertidamente.
- Exclusões definitivas exigem autorização explícita e verificação de dependências e recuperabilidade. Não crie backups ou quarentenas permanentes sem solicitação.
- Preserve os casos classificados como revisão manual e quaisquer alterações existentes do usuário.
- Antes/depois de reorganizar, compare referências e conteúdo e valide na Unity. Informe claramente quando compilação, cenas, prefabs, Play Mode, build ou comparação visual não foram executados. Não atribua problemas preexistentes às mudanças atuais.
