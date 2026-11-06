# Prompt mestre — Mapas Unity / Total Chaos

Crie, revise e otimize o mapa solicitado até validar o resultado. Prioridade: instrução atual do usuário → AGENTS.md → este prompt → .codex/SKILL.md → contexto comprovado. Respeite os limites do ambiente e preserve alterações existentes.

## Leitura incremental
- Leia AGENTS.md, este prompt e Docs/Maps/README.md. Comece pela seção Leitura rápida da ficha do mapa; abra somente outras seções necessárias. Aplique total-chaos-visual-direction pelas seções pertinentes de .codex/SKILL.md, sem releituras repetidas.
- Localize caminhos com rg --files; consulte campos, trechos e dependências afetadas. Não despeje YAML, catálogos ou logs completos. Reaproveite inspeções somente se arquivos e dependências não mudaram; registre hashes quando útil.
- Contexto conhecido: Unity 6000.5.7f1, URP 17.5.0, PC Windows. Confirme versões se mudaram. Busque informações existentes antes de perguntar; declare inferências e lacunas.

## Precisão e criação
Defina função, ocupação, layout, rotas e referências antes do acabamento. Distinga medidas de estimativas de imagens; use parâmetros corrigíveis. Meça cápsula em mundo, altura dos olhos, portas, mobiliário e circulação. Amplie arquitetura e redistribua objetos sem esticar player/props; salas e banheiros precisam comportar seu uso.

Preserve menu, conexões e chegadas. Edite/salve por Unity API, mantendo GUIDs, fileIDs, importadores e dependências conforme AGENTS.md. Corrija o delta; não substitua o mapa inteiro. Verifique ocupação, contato, clipping, colisões e aparência no jogo; contagens e análise estática não bastam.

## Direção Total Chaos
Referência: mod clássico do ModDB, sem copiar assets. Preserve identidade do lugar: arquitetura funcional, PBR distinguível, desgaste causal, silhuetas, objetos com história e variação entre ambientes. Luzes precisam de fontes plausíveis; sombras devem preservar navegação e textura. Não esconda defeitos com escuridão, sujeira uniforme ou pós-processamento. Use os 12 critérios da skill: mínimo 18/24, sem zero nos itens críticos; pontuação interna não substitui o usuário.

## Assets gratuitos
Pesquisa, download público e integração de assets gratuitos compatíveis estão autorizados no escopo da tarefa. Reutilize o inventário primeiro; consulte Docs/Maps/Referencias-e-Assets.md sob demanda. Prefira fontes primárias/CC0 e poucos candidatos focados. Confirme autor, licença, formato, escala, PBR, pivô e hitbox. Registre fonte, autor, licença, data, SHA256 e conversões em Licenses/AssetSources/<Mapa>/. Importe somente arquivos usados. Não compre/assine/crie contas sem autorização; não solicite novamente permissões já fornecidas.

## Desempenho e conclusão
Consulte unity-map-reviewer e unity-shader-prefab-optimizer quando pertinentes; preserve UVs, passes e keywords ao modificar shaders.

Diagnostique antes de otimizar; priorize ms de frame CPU/GPU, culling, luzes/sombras, geometria, materiais e colisores. Não aplique batching/LOD ou perda visual sem evidência; declare trade-offs. Confira APIs nos pacotes instalados ou documentação oficial.

Teste proporcionalmente ao delta: compilação/referências, escala/percurso, capturas, menu/portais, build e medições quando afetados. Registre hardware, resolução, condições e limites; não invente ganhos nem repita testes sem motivo. Plantas diferentes não são benchmarks equivalentes.

Atualize ficha/fontes, retire somente temporários desta tarefa após verificar dependências/regeneração e respeite AGENTS.md sobre exclusões. Não recrie backups/quarentenas/auditorias proibidos. Entregue mudanças, arquivos/capturas, testes, métricas confirmadas e pendências em resposta curta; detalhes ficam na documentação.
