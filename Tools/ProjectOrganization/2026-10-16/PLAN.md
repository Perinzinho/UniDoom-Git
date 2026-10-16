# Complemento ao plano aprovado — 16/10/2026

O plano original de 15/10 já estava aplicado. A verificação de 16/10 encontrou 5.332 arquivos de assets, sem GUIDs duplicados, metas ausentes, assets originais perdidos ou conteúdo próprio fora da estrutura aprovada.

As edições posteriores de Library, Cylinder e da entrada passaram a compartilhar 94 assets que ainda estavam dentro de cenários. Aplicar a regra GLOBAL do mesmo plano: 52 PNGs, um HDR, 19 meshes, 14 materiais, cinco modelos FBX e três prefabs.

Destino: `Assets/_Project/Global/<Tipo>/<CenárioDeOrigem>/<SubpastasOriginais>`. Preservar os nomes, conteúdo, GUIDs, fileIDs e metas; não fundir materiais ou recriar modelos. As cinco pastas de modelos contêm apenas FBX e meta; os arquivos não têm texturas externas adjacentes que precisem acompanhá-los.

Usar `AssetDatabase.MoveAsset`, registrar cada operação e atualizar os aliases de caminhos da ferramenta de Editor e o resolvedor dos verificadores históricos. As dependências por GUID acompanham os movimentos; os caminhos de texto pertinentes precisam acompanhar a nova posição.

Registrar uma linha de base atual, incluindo a SampleScene salva pelo usuário antes de fechar a Unity. Abrir todas as cenas e prefabs antes/depois, comparar dependências, componentes e bindings, verificar os hashes de todos os arquivos/metas e gerar Development Build Windows. Usar esta pasta datada para não sobrescrever os snapshots e journals da execução anterior.

Arquivar com seus metas apenas as pastas deixadas vazias por estes movimentos. Não há novo descarte de assets autorizado por evidência desta auditoria; os casos incertos e as quarentenas anteriores serão preservados.

Deixar reversão própria desta rodada, com backups e verificação de alterações posteriores. Preservar as novas revisões da Library, do Cylinder, da diretoria, do jardim e do corredor; não reaplicar os arquivos antigos de Backups.
