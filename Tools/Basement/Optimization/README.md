# Otimização do Basement — 15/10/2026

## 1. Diagnóstico

Skill fornecida instalada em `C:/Users/leo/.codex/skills/unity-shader-prefab-optimizer/SKILL.md`; conteúdo conferido por SHA-256. O `PROMPT_MESTRE.md` foi lido e aplicado como orientação para esta tarefa.

Projeto: Unity **6000.5.7f1**, URP **17.5.0**, SRP Batcher habilitado. O pipeline PC usa Deferred e o Mobile usa Forward. A inspeção e as capturas foram executadas na Unity instalada, em Windows com AMD Radeon RX 6400. Isso não estabelece a plataforma/hardware-alvo do produto.

Alvo: `Assets/prefabs/Scenario/Basement.prefab`, incluindo o nested prefab `BasementFinalSanctum`. O arquivo aberto `BasementFinalArena.prefab` não foi tratado como substituto do alvo solicitado.

| Inventário completo | Antes | Depois |
| --- | ---: | ---: |
| Transforms | 1.227 | 1.227 |
| MeshRenderers | 577 | 577 |
| MeshFilters | 571 | 571 |
| Meshes distintas nos MeshFilters | 366 | 343 |
| Materiais distintos nos renderers | 93 | 93 |
| Memória das meshes no Editor, bytes | 108.575.262 | 108.365.326 |
| Vértices nas meshes distintas | 881.367 | 879.999 |
| BoxColliders | 88 | 88 |
| LODGroups | 48 | 48 |
| Luzes / luzes ativas com sombras | 70 / 3 | 70 / 3 |

A redução medida de memória das meshes é **209.936 bytes, aproximadamente 205 KiB**, no Editor. A quantidade de geometria desenhada não foi reduzida: a contagem de vértices acima considera cada asset distinto uma única vez.

Dos 93 materiais, 37 usam `UniDoom/TotalChaosInspiredSurface`, 55 usam URP Lit e um usa GUI/Text Shader. Em **33 materiais** Total Chaos, os slots de metalicidade, oclusão e sujeira usam exatamente o mesmo objeto Texture. O código anterior solicitava leituras separadas nesses slots, com o mesmo UV. Esse é um custo potencial de fragment shader/banda; o gargalo dominante de frame time ainda não foi medido.

## 2. Ranking

| Prioridade / problema | Gravidade / impacto esperado | Risco / esforço | Como comprovar |
| --- | --- | --- | --- |
| 1. Leituras repetidas da máscara | Custo por fragmento; potencial de reduzir trabalho de textura nas superfícies cobertas | Baixo, com comparação A/B; médio | GPU Profiler/RenderDoc e frame time GPU no Player |
| 2. Meshes serializadas idênticas | Duplicação pequena de memória; redução confirmada apenas no Editor | Baixo; baixo | Inventário antes/depois e Memory Profiler no Player |
| 3. Luzes, área coberta por superfícies e submissão de renderers | Possíveis próximos gargalos; sem evidência para priorizar um deles | Mudar luzes, combinar meshes ou reduzir LOD pode alterar aparência/culling | CPU/GPU Profiler e Frame Debugger no Basement durante combate |

## 3. Implementação

- `Assets/prefabs/Scenario/Basement.prefab`: **23 referências de MeshFilter** passam a usar assets com conteúdo serializado idêntico, exceto o nome. A comparação inclui vértices, índices, UVs, submeshes, bounds e configurações de leitura. Os arquivos de mesh originais continuam existentes. Os **2.945 fileIDs** do prefab foram preservados; nenhum outro campo de componente mudou.
- `Assets/TotalChaos/Shaders/TotalChaosSurfaceInput.hlsl`: caminho específico para máscara compartilhada. Reutiliza uma leitura para metalicidade, smoothness, oclusão e cavidade, mantendo as conversões de precisão do URP 17.5, albedo, normais, emissão, detail e clear coat. As três leituras lógicas da máscara nos passes de cor passam a uma quando os recursos correspondentes estão ativos; isso não representa uma medição de instruções GPU ou FPS. O caminho nativo continua disponível para texturas diferentes.
- `Assets/TotalChaos/Shaders/TotalChaosInspiredSurface.shader`: keyword local `_TC_SHARED_MASK` nos passes ForwardLit, GBuffer e Meta. Os demais passes, estados de renderização e o layout `UnityPerMaterial` permanecem preservados. A keyword acrescenta uma dimensão de variantes nesses três passes; o impacto total em tamanho/tempo de compilação não foi comparado com um build anterior.
- `Assets/TotalChaos/Editor/TotalChaosMaterialConverter.cs`: o conversor e o validador do inspector calculam a keyword verificando a identidade das três texturas. Se ferramentas de runtime trocarem esses slots por texturas diferentes, precisam desativar `_TC_SHARED_MASK`; os scripts atuais do Basement não fazem essa troca.
- 33 materiais do Basement: ativação da keyword, preservando os demais campos serializados e os GUIDs. Materiais não foram fundidos, pois os arquivos de origem são usados pelo fluxo de reversão do conversor.
- `Assets/TotalChaos/Editor/BasementPerformanceAudit.cs`: auditoria, capturas, otimização via API de prefab e validação de build. Menu `Tools > Total Chaos > Optimize Basement Assets`. Os resultados são escritos em `.utmp/BasementOptimization`, fora de Assets.
- `Tools/Basement/Optimization/verify_basement.py`: verificação estrutural e comparação das imagens. Executar a partir da raiz com `py Tools/Basement/Optimization/verify_basement.py`.

Os diffs do controle de versão contêm a implementação completa. A otimização não remove componentes nem altera luzes, colliders, gameplay, hierarquia, materiais atribuídos, qualidade de texturas ou thresholds de LOD. A Unity foi usada para aplicar e carregar o prefab; o diff final preserva a ordem original dos objetos e remove apenas alterações incidentais de serialização/importação.

## 4. Testes e rollback

- Compilação de scripts e renderização executadas na Unity 6000.5.7f1; nenhuma mensagem de erro de shader nas variantes exercitadas pelas câmeras.
- Capturas em 1280 × 720: arena, portão e boss, usando os pipelines PC e Mobile. O teste Mobile executou no computador, sem validar uma API/dispositivo móvel.
- Comparação direta entre a keyword ligada e desligada na mesma sessão: **quatro capturas idênticas pixel a pixel**, cobrindo arena e boss nos dois pipelines.
- Comparação entre as sessões antes/depois completas: boss e portão idênticos nos dois pipelines; arena com **4 pixels diferentes no PC e 1 pixel no Mobile**, em 921.600 pixels, junto à borda esquerda. A origem dessa diferença residual não foi isolada. Não se afirma identidade absoluta entre as sessões.
- Verificação estrutural: todos os fileIDs preservados; somente as 23 referências a meshes comprovadamente equivalentes mudaram. Todos os campos de física e scripts permanecem iguais.
- Simulação de comportamento em Edit Mode: 12 inimigos, um sobrevivente mantém o portão fechado, última morte libera o portão, boss excluído e folha do portão levantada. Esse teste não substitui Play Mode.
- O resultado do Development Build Windows e seus detalhes estão registrados em `validation.txt`.

Capturas completas, inventários e logs: `.utmp/BasementOptimization`. [Comparação visual PC](comparison.png). Os logs contêm também avisos do projeto/pacotes, que devem ser distinguidos de erros de compilação.

Para reverter as alterações nos assets e no código existente, use `git apply --check -R Tools/Basement/Optimization/changes.patch` e, se a verificação passar, `git apply -R Tools/Basement/Optimization/changes.patch`. O patch não remove os novos arquivos de auditoria/relatório. Também há backup local do prefab e de materiais em `.utmp/BasementOptimization`; não sobrescreva edições posteriores ao reverter.

Ainda necessário para uma comparação de desempenho do produto: executar Player antes/depois no hardware-alvo, mesma resolução/qualidade/câmera, após aquecimento, com a mesma carga de inimigos. Coletar CPU main/render thread e GPU em **ms**, GC allocations, memória, SetPass, batches/draw calls, triângulos e overdraw. Validar em Play Mode o percurso, portão, iluminação oscilante, atmosfera e combate. Conferir Frame Debugger, depth/depth normals, sombras e motion vectors nas configurações efetivamente usadas. Lightmap bake e XR não foram testados.

Referências de API: [edição isolada de prefabs](https://docs.unity.cn/6000.0/Documentation/ScriptReference/PrefabUtility.LoadPrefabContents.html); implementação e precisão conferidas diretamente em `Library/PackageCache/com.unity.render-pipelines.universal@73b4c4ff130e/Shaders/LitInput.hlsl`, versão 17.5.0 instalada.

## 5. Veredito

**Confirmado:** skill instalada; 23 referências consolidadas; 23 meshes distintas a menos; aproximadamente 205 KiB a menos nas meshes medidas no Editor; shader A/B idêntico nas quatro capturas testadas; verificações de referência e comportamento aprovadas.

**Potencial:** menor custo de textura/fragment shader nas superfícies dos 33 materiais compatíveis. **Não medido:** melhoria de FPS, frame time CPU/GPU, memória do Player, draw calls e comparação de tamanho/variantes entre builds. Não há percentual de ganho de desempenho comprovado.
