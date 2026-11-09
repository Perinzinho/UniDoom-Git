# Porão e prisão — ficha rápida

## Leitura rápida

- Cena: `Assets/_Project/Scenes/Basement/Basement.unity`.
- Prefab principal: `Assets/_Project/Scenes/Basement/Prefabs/Basement.prefab`.
- Cadeia carregada pelo jogo: `Basement.unity` → `BasementScenario.prefab` → `Basement.prefab`. A variante tem apenas dois overrides na área da prisão, ambos na continuação da parede do santuário; não sobrescreve o layout corrigido das celas.
- Área das celas: `Assets/_Project/Scenes/Basement/Prison/`.
- Arte da transição: `Assets/_Project/Scenes/Basement/Resources/Textures/UI/Transitions/Basement.png`, derivada da visão real de chegada `FromCampus` e tratada no mesmo padrão cinematográfico de `Cylinder`.
- Quantidade atual: 13 celas e 13 professores.
- Chaves: treze IDs próprios, um por cela, distribuídos entre Direito, Biblioteca e Bloco 4. A HUD lista todos os treze. Mapa de locais em [PrisonCellKeys.md](PrisonCellKeys.md).
- Ocupação: exatamente um professor por cela; cada porta referencia somente o professor e a voz da própria cela.
- Módulo compacto: passo de **3,20 m**, com área livre aproximada de **3,00 × 3,48 m**. Sete celas ficam à esquerda e seis à direita; conjunto de 22,40 m ao longo do corredor. Folha móvel de aproximadamente 1,47 m e trecho fixo de aproximadamente 1,41 m, alinhados dentro de cada módulo. Divisórias de 20 cm; corredor central livre de aproximadamente 2,97 m.

## Professores

O mapeamento salvo no prefab principal é:

| Cela | Professor | Voz |
|---:|---|---|
| 01 | Caio | `caio_line.wav` |
| 02 | Fábio | `fabio_line.wav` |
| 03 | Henrique | `henrique_line.wav` |
| 04 | Matheus Santi (prefab anterior `Teacher-santi`) | `santi_line.wav` |
| 05 | Navarro | arquivo de voz ainda não fornecido |
| 06 | Matheus Ferraroni | arquivo de voz ainda não fornecido |
| 07 | Paulinho | `paulinho_line.wav` |
| 08 | Rafa | `rafa.wav` |
| 09 | Gustavo Camossi | `gustavo-camossi.wav` |
| 10 | Matheus Santi | `matheus-santi.wav` |
| 11 | Moacyr | `moacyr.wav` |
| 12 | Querino | `querino.wav` |
| 13 | William | `william.wav` |

Cada `PrisonCellGate` referencia o transform do único professor de sua cela. `PrisonController` consulta o `AudioSource` desse transform ao aceitar a abertura; os clipes não tocam ao carregar a cena (`Play On Awake` desabilitado). Não atribua falas de outras pessoas às celas 05 e 06: elas devem permanecer sem clipe até os arquivos corretos serem fornecidos.

As treze imagens originais mantêm seu canvas 1536 × 1024 e seus bytes. Cada pose parada usa recorte próprio com margem de quatro pixels e pivô nos pés; PPU ajustado para altura visível de aproximadamente 1,75 m. Os oito sprites RGB usam o material de remoção do fundo preto já existente. Os cinco sprites RGBA usam `Prison/Materials/TeacherTransparent.mat`, com alpha da imagem habilitado e remoção de preto desabilitada, preservando roupas e cabelos escuros. GUIDs e IDs dos sprites foram mantidos. As vozes foram normalizadas anteriormente como WAV PCM mono, 44,1 kHz; esta revisão visual não modifica áudios.

## Regra de capacidade

Distribua exatamente um professor por cela. Novos professores exigem novo módulo compacto de 3,20 m, porta, `PrisonCellGate`, ID único, placa e referência exclusiva ao professor/áudio. Com quantidade ímpar, um dos lados do corredor pode terminar com uma cela adicional e o lado oposto deve receber fechamento sólido.

## Validações anteriores — histórico

Em 2026-11-09, o prefab foi reorganizado pela Unity para 13 celas compactas. A validação do asset confirmou 13 `PrisonCellGate`, IDs únicos de 1 a 13, 13 referências distintas de professor e os 11 clipes disponíveis ligados às pessoas corretas. Navarro e Matheus Ferraroni não possuem arquivos de voz no projeto nem nos anexos recebidos. Capturas de revisão em 1600 × 900 foram feitas na entrada e no meio do corredor; grades, iluminação, fechamento do lado par, piso, teto e rota central permaneceram legíveis. Compilação final e teste de abertura em Play Mode ainda devem ser registrados após retirar a ferramenta temporária.

Em 2026-11-09, a transição global passou a bloquear o jogador até concluir o aquecimento dos shaders e ganhou arte 16:9 própria para o porão. A captura-fonte foi renderizada na chegada real `FromCampus`, sem alterar cena, prefab, layout ou spawn.

Em 2026-11-09, a Unity importou os cinco sprites, criou os cinco prefabs e confirmou 13 professores no prefab principal. A revisão estática confirmou uma instância de cada professor, GUIDs únicos, referências de sprite e áudio válidas e ausência de fileIDs duplicados. Após a restauração das vozes, o piso de ruído medido passou de aproximadamente −31/−39 dB para −72/−75 dB nos clipes com fundo audível; os picos finais ficaram entre −3,3 e −1,5 dB. A Unity reimportou os cinco WAVs sem erros, mantendo seus GUIDs. O Play Mode do porão e uma comparação auditiva dentro das celas não foram executados após o delta final minimizado.

## Correções da integração de development — 09/11/2026

O log existente `Logs/Editor.log` mostrou `CS0200` em `PrisonPlayValidation.cs`: a ferramenta tentava atribuir `PrisonProgress.HasKey`, propriedade somente de leitura. A ferramenta agora preenche seu inventário de validação com `PlayerCellKeys.TryAddKey`, conforme os IDs das celas. Sua saída automática do editor ficou restrita ao modo batch; não fecha uma sessão interativa.

A expansão havia criado as celas 9–13 mantendo a progressão limitada a oito. Após autorização do usuário, `PrisonProgress.CellCount = 13` passou a definir o limite comum de IDs, gates, pickups e HUD; cinco novas chaves foram distribuídas. O ícone de fallback da HUD reutiliza a chave neutra existente; os novos pickups têm o número na plaqueta.

A cena usa `BasementScenario.prefab`, que instancia `Basement.prefab`. Conferidas a cadeia de referências e a ausência de overrides do ícone. Prefabs recebidos e modificados foram conferidos por IDs/referências; áudios e imagens recebidos contêm dados completos, sem ponteiros LFS pendentes.

Nesta correção, nenhuma Unity, compilação, jogo, Play Mode, build, ferramenta de validação ou captura foi iniciada. O diagnóstico usa o log já existente e inspeção dos arquivos. Sem commit ou push.

## Isolamento das celas e sprites completos — 09/11/2026

Problemas confirmados nos arquivos:

- Nove malhas agrupadas do layout antigo continuavam visíveis junto às treze celas compactas. Havia paredes, grades e sinalização antigas sobrepostas à arquitetura nova.
- A construção compacta posicionava portas e grades como se o pivô estivesse no centro, mas os segmentos se estendem para o lado positivo de Z a partir da dobradiça. A grade fixa ultrapassava o limite da cela seguinte.
- Um mesmo recorte de 126 × 284 pixels era usado em desenhos com posições e tamanhos diferentes. Braços e pés ficavam fora do retângulo; o recorte de Querino começava em x=189, enquanto sua pose começa perto de x=92.
- Os cinco PNGs novos já continham alpha, mas o material compartilhado estava configurado para ignorá-lo, deixando fundos visíveis.

Correções salvas:

- Renderers das nove malhas antigas desativados; assets e IDs preservados. Pisos, paredes externas, teto, passagem, fechamento do lado par e mesa de triagem receberam renderizadores de cubo nas geometrias dos próprios colisores existentes. Materiais anteriores reutilizados. Cinco difusores restaurados junto às luzes existentes. Sem alterar luzes ou colisores dessas superfícies.
- Treze frentes alinhadas ao início de cada módulo. Folha em `startZ + 0,23`, escala Z=0,64; trecho fixo em `startZ + 1,75`, escala Z=0,60. Colisores ajustados às travessas visíveis. As grades deixam de invadir a próxima cela. A abertura foi limitada a 90° para a folha não atravessar a divisória ao terminar o giro.
- Treze professores posicionados em `x = ±3,10`, `y = 0,02`, `z = startZ + 1,10`, com folga para paredes e bancos, um professor por cela. Referências de professor, voz, chave e interação preservadas.
- Treze importadores receberam recortes completos e altura consistente. Cinco prefabs passaram a usar o material que respeita o alpha. Nenhuma imagem original foi substituída ou editada; hashes dos treze PNGs conferidos contra os objetos LFS registrados no Git.
- Ferramenta de layout ajustada para os mesmos offsets e para remover todos os módulos anteriores antes de reconstruir, evitando duplicar celas e placas em uma regeneração futura. A ferramenta não foi executada.

Conferência estática: IDs únicos, referências locais válidas, treze portas/treze professores, folhas contidas nos próprios módulos e envelopes de sprites afastados das paredes. GUIDs, IDs de subassets, vozes, cena e variante preservados. Não houve Unity, Play Mode, compilação, build, captura, vídeo, medição de FPS, commit, push ou desligamento. Aparência, circulação e abertura em execução precisam de conferência do usuário.

## Remoção de móveis e latas abertas — 09/11/2026

Retiradas da área jogável 11 instâncias de estrados/camas e colchões antigos, desativando suas raízes. Os treze bancos retangulares e a mesa de triagem também ficaram inativos. Objetos serializados e assets originais preservados; paredes, pisos, grades, professores, pickups e suas referências não foram alterados nesta etapa. A ferramenta de layout mantém os bancos desativados.

Adicionado `Prison/Prefabs/OpenFoodCan.prefab`: lata de alimento aberta enferrujada, 18 cm de altura incluindo a tampa, 60 triângulos, textura e normal map de 256 × 256. Dezesseis instâncias junto às laterais internas: uma em cada cela, com uma extra nas celas 03, 06 e 09, sem colisores e sem ocupar o corredor ou o espaço dos professores. Malha nativa Unity, sem dependência de Blender instalado.

Modelo **open can (junk)** de **p0ss**, [OpenGameArt](https://opengameart.org/content/open-can-junk), CC0 1.0. Autor incluído em `CreditsContent.txt` e `Docs/Maps/creditos.md`, sem links nos créditos exibidos. Origem, licença, hashes e conversão em `Licenses/AssetSources/Basement/open-food-can.json`.

Conferência estática de referências, IDs, malha e posicionamento. Unity, compilação, Play Mode, build, comparação visual, screenshots e vídeo não executados. Sem commit, push ou desligamento.
