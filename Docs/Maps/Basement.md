# Porão e prisão — ficha rápida

## Leitura rápida

- Cena: `Assets/_Project/Scenes/Basement/Basement.unity`.
- Prefab principal: `Assets/_Project/Scenes/Basement/Prefabs/Basement.prefab`.
- Área das celas: `Assets/_Project/Scenes/Basement/Prison/`.
- Arte da transição: `Assets/_Project/Scenes/Basement/Resources/Textures/UI/Transitions/Basement.png`, derivada da visão real de chegada `FromCampus` e tratada no mesmo padrão cinematográfico de `Cylinder`.
- Quantidade atual: 13 celas e 13 professores.
- Ocupação: exatamente um professor por cela; cada porta referencia somente o professor e a voz da própria cela.
- Módulo compacto: 3,20 m de largura por aproximadamente 3,65 m de profundidade. Sete celas ficam à esquerda e seis à direita; o conjunto ocupa 22,40 m no eixo do corredor, apenas cerca de 2,20 m além do arranjo anterior de oito celas.

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

Todos os cinco sprites usam canvas 1536 × 1024, transparência, 160 pixels por unidade e o mesmo recorte de repouso dos professores existentes. As vozes foram normalizadas como WAV PCM mono, 44,1 kHz, com corte de frequências fora da faixa vocal, redução adaptativa de ruído, compressão leve e normalização para −16 LUFS/−1,5 dBTP.

## Regra de capacidade

Distribua exatamente um professor por cela. Novos professores exigem novo módulo compacto de 3,20 m, porta, `PrisonCellGate`, ID único, placa e referência exclusiva ao professor/áudio. Com quantidade ímpar, um dos lados do corredor pode terminar com uma cela adicional e o lado oposto deve receber fechamento sólido.

## Validação desta revisão

Em 2026-11-09, o prefab foi reorganizado pela Unity para 13 celas compactas. A validação do asset confirmou 13 `PrisonCellGate`, IDs únicos de 1 a 13, 13 referências distintas de professor e os 11 clipes disponíveis ligados às pessoas corretas. Navarro e Matheus Ferraroni não possuem arquivos de voz no projeto nem nos anexos recebidos. Capturas de revisão em 1600 × 900 foram feitas na entrada e no meio do corredor; grades, iluminação, fechamento do lado par, piso, teto e rota central permaneceram legíveis. Compilação final e teste de abertura em Play Mode ainda devem ser registrados após retirar a ferramenta temporária.

Em 2026-11-09, a transição global passou a bloquear o jogador até concluir o aquecimento dos shaders e ganhou arte 16:9 própria para o porão. A captura-fonte foi renderizada na chegada real `FromCampus`, sem alterar cena, prefab, layout ou spawn.

Em 2026-11-09, a Unity importou os cinco sprites, criou os cinco prefabs e confirmou 13 professores no prefab principal. A revisão estática confirmou uma instância de cada professor, GUIDs únicos, referências de sprite e áudio válidas e ausência de fileIDs duplicados. Após a restauração das vozes, o piso de ruído medido passou de aproximadamente −31/−39 dB para −72/−75 dB nos clipes com fundo audível; os picos finais ficaram entre −3,3 e −1,5 dB. A Unity reimportou os cinco WAVs sem erros, mantendo seus GUIDs. O Play Mode do porão e uma comparação auditiva dentro das celas não foram executados após o delta final minimizado.
