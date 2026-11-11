# Otimização de desempenho — 10/11/2026

## Escopo e preservação

Branch de trabalho: `feat/optimization`. O usuário autorizou implementar as otimizações justificadas e fazer commit/push ao terminar. Não reduzir resolução, sombras, iluminação, efeitos, distância de visão, precisão física ou frequência da IA para obter números melhores. Preservar nomes públicos, campos serializados, GUIDs e fileIDs. Mudanças independentes serão verificadas isoladamente. Os MDs, skills e prompts anteriores permanecem preservados.

Diagnóstico inicial somente por leitura. Unity **6000.5.7f1**, **URP 17.5.0**, perfil **PC/Standalone**, renderer PC em modo serializado 2, render scale 1, HDR e SSAO ativos, SRP Batcher ativo, dynamic batching desativado; física com passo de 0,02 s, autoSyncTransforms desativado e matriz de colisões liberada. Esses valores não serão alterados nesta revisão.

Não foi localizado um capture atual de Profiler/GPU/Memory Profiler no projeto. Os números de LawSchool em 05/11 (RX 6400, D3D12, 1600×900) são históricos, explicitamente anteriores à revisão atual; não são baseline desta tarefa. O contador de FPS do menu não substitui um capture com CPU/GPU/GC. Os 276 campos de referência pendentes detectados na limpeza são uma limitação preexistente da representatividade visual dos mapas, fora do escopo desta otimização.

## Diagnóstico e plano

As evidências abaixo são de código/configuração; custos em milissegundos e ganhos de FPS ainda não estão medidos. Contagens serializadas incluem objetos inativos e não equivalem a draw calls ou colliders ativos.

| Sistema/arquivo | Evidência e impacto estimado, sem promessa de FPS | Proposta | Risco/efeitos a controlar | Validação |
| --- | --- | --- | --- | --- |
| `Global/Scripts/Utils/OpenDoorWithKey.cs` | `Update` converte método de instância em predicate para `RemoveWhere`, inclusive sem jogador no trigger. Trabalho/alocação repetida; benefício estimado pequeno por porta, acumulável quando há muitas | Reutilizar o predicate e pular a remoção quando o conjunto está vazio | A: preservar limpeza após teleporte/destruição, regras de contato/chaves, timers e animação | Bytes gerenciados e tempo do método; jogador presente/ausente, stale collider, contato e chaves |
| `LawSchool/Scripts/LawSchoolMirror.cs` | Teste de visibilidade usa overload que retorna um novo `Plane[6]`, antes do limite de 15 Hz. Possível GC mesmo sem render novo | Buffer de seis planos por espelho, usando o mesmo cálculo | A: mesma visibilidade, resolução, frequência e reflexão | Bytes/tempo no caminho limitado; comparação dos planos e da decisão de visibilidade |
| `LawSchool/Scripts/LawSchoolAlarmPrototype.cs` | `TintActors` calcula proximidade/cone e faz linecast também na fase vermelha, mas nessa fase a cor final usa somente `actor.color * SpriteRed` | Retornar a cor vermelha por ator antes de calcular dados utilizados somente no apagão | A: não mudar cores, fases, cronômetros, spawn, lanterna ou regras do apagão | Comparação de cores em vermelho/apagão e lanterna ligada/desligada; CPU do mesmo conjunto de atores |
| `Global/Scripts/Utils/CampusGrounding.cs` | `TryFloor` usa `RaycastAll` e é chamado pela movimentação física de inimigos no campus. Já há NonAlloc nas consultas internas de Enemy; este helper ainda retorna arrays novos | Buffer reutilizável com crescimento em saturação, preservando filtros e seleção do suporte; fallback original em empates | B: nenhuma perda de hits nem alteração da escolha de chão/chegada | Comparar collider, ponto, normal, distância e resultado com a consulta original; múltiplos pisos, empates, triggers e saturação; bytes/tempo |
| `Global/Scripts/UI/DoorReloadScreen.cs` | Toda transição chama aquecimento de coleção e `Shader.WarmupAllShaders`; possível pico de carregamento, não custo contínuo de FPS | Medir duração/primeiro frame antes de propor cache ou coleção específica | B: retirar warmup pode devolver stutter no primeiro uso; manter nesta revisão sem prova | Timeline da transição e primeiro uso das armas/materiais em build |
| IA: `Enemy.cs` e navegação | Percepção e caminhos já usam intervalos 0,15 s e 0,25 s no prefab; separação usa NonAlloc. Desvios podem testar várias direções quando há bloqueios | Medir encontros densos antes de qualquer alteração da IA | B/C: ordem ou intervalo diferente pode alterar perseguição, tiros e contato. Não aplicar throttling arbitrário | CPU Timeline, trajetórias, ataques e encontros equivalentes |
| Gráficos/iluminação/geometria | Prefab Bloco 4: 5.970 MeshRenderers e 438 lights serializados; Direito: 3.112 e 314; biblioteca do campus: 1.440 e 79. Não são contagens visíveis. Copa LOD0 possui 397.555 triângulos e já há 124 LODGroups no exterior | Medir draw calls, SetPass, sombras, SSAO, overdraw e LOD real no enquadramento antes de escolher alterações | C: preservar aparência; nenhuma redução global de qualidade, geometria ou sombras nesta revisão | Profiler GPU/Frame Debugger em build e comparação visual equivalente |
| Memória/texturas/áudio | Tamanho do arquivo fonte não é tamanho em RAM/VRAM. Ex.: `HiddenKing.wav` tem cerca de 201 MiB e agora é a música fixa dos créditos, com fallback em `Resources/Audio/Credits/HiddenKing`; não inferir residência apenas pelo tamanho/referência | Medir recursos realmente carregados e seus importadores no cenário real | C para compressão/streaming/descarregamento: pode afetar arte, som e latência. Não alterar sem evidência | Memory/Audio Profiler e transições repetidas |

Já preservados como soluções existentes: HUD de vida/munição por eventos; pool de AudioSources em `SFXPlayer`; cache dos bancos de passos; `RaycastNonAlloc` nos passos; cache de sprites/retângulo no HUD das armas; buffer de frustum, distância, visibilidade e atualização limitada em `ParallaxPortalView`; limites de frequência nos espelhos e na IA. Não introduzir pooling ou refatorações gerais sem um gargalo demonstrado.

## Medições e resultados

Executado em Unity 6000.5.7f1, Windows 11, aproximadamente 8 GB de RAM informados pelo Editor, em **batch `-nographics` / Null Device**. São microbenchmarks de CPU em métodos isolados, sem renderização: **não medem FPS, GPU, VRAM ou fluidez de um executável**. Foram feitas 128 chamadas de aquecimento e cinco repetições por amostra. O tempo abaixo é a mediana do lote inteiro, não o tempo de um frame do jogo.

| Método/cenário | Chamadas por lote | Original, ms | Otimizado, ms | Interpretação |
| --- | ---: | ---: | ---: | --- |
| Porta vazia | 10.000 | 6,6926 | 6,1652 | Processos distintos e intervalos sobrepostos; ganho de CPU não conclusivo |
| Porta com jogador sobreposto | 10.000 | 26,5616 | 23,4977 | Processos distintos e intervalos sobrepostos; ganho de CPU não conclusivo |
| Espelho visível, atualização limitada | 10.000 | 31,7862 | 33,3629 | Sem ganho de CPU demonstrado; mantido pela eliminação da criação repetida do array de planos, com cálculo idêntico |
| Alarme vermelho, mesmos oito atores | 2.000 | 55,0747 | 3,8730 | Original e otimizado na mesma execução, mesmos renderers/câmera/colisor e ordem alternada; redução clara neste trecho isolado |
| Alarme no apagão, controle | 2.000 | 67,9526 | 55,9147 | Intervalos amplamente sobrepostos; nenhum ganho atribuído ao apagão, cujo cálculo permanece igual |
| Piso em posições reais do campus | 2.000 | 14,9618 | 14,1192 | Comparação pareada na mesma execução, intervalos sobrepostos; ganho de CPU não conclusivo |

No vermelho, os cinco lotes originais ficaram em **47,9005–73,5695 ms**, contra **2,3986–3,9255 ms** após a mudança. Isso sustenta a redução do trabalho do método nesta fixture; não sustenta promessa de aumento percentual de FPS. No apagão, os intervalos foram **54,5030–85,8569 ms** e **53,9537–85,0358 ms**, respectivamente. No piso, **14,4568–18,0723 ms** e **13,0700–17,9228 ms**. A variação observada impede apresentar pequenas diferenças das medianas como melhorias comprovadas do jogo.

### Memória/GC: resultados descartados da conclusão

`GC.GetAllocatedBytesForCurrentThread` retornou zero também para uma alocação de calibração conhecida. O Editor recusou a troca de modo do coletor. A alternativa por delta de `Profiler.GetMonoUsedSizeLong` possui granularidade e inclui outras atividades/threads; seus valores ficaram apenas como diagnóstico local e **não são um contador confiável de bytes alocados por chamada**. Não foram publicados números de redução de GC, RAM ou VRAM.

As mudanças estruturais verificáveis são: um predicate vinculado por porta em vez da conversão em cada Update; um `Plane[6]` por espelho em vez do array retornado a cada teste; buffer de hits reutilizado no piso, que cresce quando necessário. Empates de suporte mantêm a consulta original e podem continuar alocando. Nenhuma alegação de “zero alocação” para o jogo foi feita.

## Alterações aplicadas

| Arquivo sob `Assets/_Project/` | Mudança final e preservação |
| --- | --- |
| `Global/Scripts/Utils/OpenDoorWithKey.cs` | Predicate inicializado no Awake e reutilizado; conjunto vazio não chama RemoveWhere. Mantidos contato, chaves, teleporte, timers, direção, animação e sons |
| `Scenes/LawSchool/Scripts/LawSchoolMirror.cs` | Array de seis planos por instância e overload com destino. Mantidos cálculo do frustum, teste de visibilidade, 15 Hz, distância, resolução, reflexão e configuração de câmera |
| `Scenes/LawSchool/Scripts/LawSchoolAlarmPrototype.cs` | Na fase vermelha, aplica a mesma cor antes dos cálculos que só influenciavam o apagão. Mantidos o cálculo do apagão, lanterna, fases, eventos e spawns |
| `Global/Scripts/Utils/CampusGrounding.cs` | NonAlloc com buffer inicial de 32 hits, crescimento e repetição em saturação; mantém todos os filtros e o suporte mais próximo. Em empate da distância mínima, usa RaycastAll para preservar a escolha original. TryArrival, assinaturas públicas, máscara e triggers preservados |

Documentação nova desta tarefa: `Docs/Performance-Optimization.md`. Nenhuma configuração gráfica, textura, material, áudio, cena, prefab, `.meta`, campo serializado ou contrato público foi intencionalmente modificado. Backup recuperável das quatro fontes originais confirmado por comparação de bytes com o commit `43cc1999` antes de editar. Cada otimização foi salva em commit separado, depois da validação isolada.

## Validação funcional

- Baseline original: **96 verificações** com as fontes originais.
- Portas: **7 verificações**, incluindo presença/ausência, teleporte sem OnTriggerExit, collider desativado, contato e chave obrigatória.
- Espelhos: **3 verificações**, incluindo igualdade dos planos, decisão de visibilidade e manutenção do limite de atualização sem criar câmera no caminho limitado.
- Alarme: **64 verificações de cor**, cobrindo oito atores, vermelho/apagão e lanterna ligada/desligada; comparações com a fórmula anterior e com a classe original temporária usando os mesmos renderers.
- Piso: **54 verificações**, incluindo grade de 49 posições da cena CampusExterior, uso de suportes reais no benchmark, 80 hits para exigir crescimento, pisos coplanares, trigger ignorado e ausência de suporte. Comparados resultado, collider, ponto, normal e distância com a implementação original.
- Regressão conjunta após as quatro alterações: **96 verificações aprovadas**, em execução separada após os testes isolados.

As ferramentas de verificação são temporárias e não fazem parte do jogo. A comparação do alarme utilizou uma cópia da fonte original somente como fixture, com nome distinto, sem salvar GameObjects/cenas. Uma tentativa de colocar essa fixture em Editor foi recusada pela Unity e corrigida antes do teste pareado. As tentativas de calibração de GC e essa adaptação do verificador não foram falhas do gameplay; os testes finais compilam as fontes aprovadas. Registros locais ignorados pelo Git: `Logs/PerformanceOptimization-{baseline,door,mirror,alarm,ground,final}.{json,log}`.

### Integridade final

- Regressão conjunta: **96 verificações aprovadas**; compilação final **concluída com código de saída 0**, após retirar o verificador e a fixture original da árvore Assets.
- **19.167 arquivos preservados comparados por SHA-256** com o inventário da limpeza anterior: **zero alterações inesperadas e zero arquivos faltando**, excluindo somente as quatro fontes intencionalmente otimizadas. Isso inclui cenas, prefabs, materiais, meshes, texturas, áudio, `.meta`, configurações, documentação anterior, skills e prompts presentes no workspace.
- Declarações públicas e campos `[SerializeField]` das quatro fontes comparados com a versão recuperável original: sem alteração de contrato. Nenhum asset foi movido, renomeado ou excluído por esta etapa de otimização.
- Commits isolados: portas `ea099a48`, espelhos `5f859a6b`, alarme `f8f342f9`, piso `362ebdd0`. As alterações locais alheias a esta tarefa foram preservadas e não fazem parte desses commits.
- As ferramentas pontuais foram retiradas após conferir ausência de consumidores no código; a compilação seguinte confirmou que o projeto não depende delas. Os resultados persistentes são o código do jogo e este relatório.

Não foram executados: Play Mode, sessão manual de movimentação/combate, testes auditivos, travessias completas de portais, comparações de pixels renderizados, captura de GPU/Memory Profiler ou build executável nesta revisão. Os testes de porta e piso são funcionais no Editor, não uma sessão completa do jogador. A ausência de mudança visual dos espelhos não foi demonstrada por screenshot: foram preservados o cálculo e os parâmetros e comparada a visibilidade. As 276 referências pendentes da inspeção anterior não foram corrigidas nem atribuídas a esta tarefa.

## Oportunidades preservadas para investigação

Não aplicados sem medição suficiente: troca de pipeline/render mode, sombras, SSAO, render scale, texturas/streaming, compressão de áudio, remoção do warmup global de shaders, pooling adicional, alterações dos intervalos da IA, matriz global de colisões, simplificação de colliders, modelos, LODs e frequência de reflexos. A autorização para otimizar não elimina a exigência de preservar comportamento e aparência; esses ajustes não receberam uma justificativa mensurada nesta rodada.

## Limitações e próximas verificações

Antes de afirmar ganho de FPS: medir uma build com janela efetivamente renderizando, resolução e qualidade fixas, percurso e inimigos equivalentes, aquecimento e amostras de frame time (mediana/p95/p99), CPU/GPU e GC. Não comparar batch `-nographics` com jogo renderizado. Não corrigir as referências pendentes nem alterar assets/cenas como parte desta revisão.
