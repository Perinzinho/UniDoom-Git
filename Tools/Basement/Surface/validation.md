# Validação do Basement — 13/10/2026

Unity 6000.5.7f1, URP 17.5, projeto isolado `.utmp/BasementPreview`.

## Executado

- Compilação C# e renderização real do shader, sem erros de compilação ou Render
  Graph. Diagnóstico de compatibilidade SRP Batcher aprovado.
- 179 renderizadores PBR; todas as referências de albedo, normal, máscara e AO
  presentes. Reversão das referências de materiais verificada no prefab carregado.
- Cinco capturas finais: arena, tubulações, sala final, lanterna e corredor.
  A fração de pixels abaixo de 0.025 de luminância na arena caiu de 59.91% para
  2.85%; na sala final, de 61.83% para 5.19%, com as mesmas câmeras sem lanterna.
  Métricas de imagem em `capture-metrics.txt`; não são medições de FPS.
- Dimensões, paredes e teto verificados no prefab e na cena; ausência de intrusão
  de outras salas; 12 pontos de spawn e marcador de boss preservados.
- CharacterController real percorreu entrada, arena, portão, corredor em L,
  centro e laterais da sala final, incluindo retorno, no prefab e no mapa.
  O portão fechado bloqueou a passagem; as portas existentes foram verificadas
  em proximidade, bloqueio e ambos os sentidos de abertura.
- Simulação do combate existente: ativação gera 12 inimigos; um sobrevivente
  mantém o portão fechado; a última morte libera e levanta a colisão do portão;
  o boss permanece fora do encontro da arena.
- Play Mode com câmera real nos quatro volumes do Basement. Névoa, pós local e
  lanterna com sombras validados; ambiente, luzes direcionais e configurações da
  câmera restaurados ao sair. Scripts de movimentação/combate foram suspensos
  nesta verificação visual; foram testados separadamente acima.
- Overrides reais do corredor considerados: centro do piso no mapa
  `(72.44, 1.86, -5.38)` e centro do volume `(72.44, 4.61, -5.38)`.
  O piso do prefab isolado mantém suas dimensões originais. Apenas os limites dos
  volumes e seus triggers acompanham a geometria de cada instância.
- Dois probes locais vinculados a cubemaps estáticos de 128 px; três luminárias
  com sombras, além da lanterna. Parallax e recaptura de reflexos desativados.

Logs: `.utmp/Basement-PBR-final.log` e `.utmp/Basement-PBR-play.log`.
Marcadores finais: `BASEMENT_SURFACE_FINAL_OK`, `BASEMENT_SURFACE_SRP_BATCHER_OK`,
`BASEMENT_SURFACE_GAMEPLAY_OK`, `BASEMENT_ENCOUNTER_TEST_OK` e
`BASEMENT_SURFACE_PLAYMODE_OK`.

## Revisão visual pela skill

Avaliação artística das capturas, com evidência técnica complementar: **21/24**.

| Critério | Nota | Observação |
|---|---:|---|
| Escala e arquitetura | 2 | Dimensões e percurso existentes validados |
| Materiais distinguíveis | 2 | Concreto, pedra e metais com normal e rugosidade próprios |
| Desgaste causal | 1 | Danos e infiltrações existentes; alguns padrões de textura se repetem |
| Fontes e direção da luz | 2 | Luminárias físicas, pools locais e fontes de teto direcionadas |
| Sombras e navegação | 2 | Rotas visíveis sem lanterna; contraste preservado |
| Profundidade e composição | 2 | Pilares, tubulações, névoa distante e foco no altar |
| Uso anterior e abandono | 2 | Bombas, barris, grades, equipamentos e rastros preservados |
| Escala, contato e colisão dos props | 2 | Geometria anterior preservada e percurso real verificado |
| Legibilidade dos detalhes | 1 | Materiais mais nítidos; textos mundiais antigos ainda têm limitações |
| Identidade do cenário | 2 | Instalação subterrânea fria com acentos âmbar e sala ritual |
| Pós-processamento | 2 | Grain, bloom e vinheta reduzidos; materiais funcionam sem filtros fortes |
| Desempenho | 1 | Orçamento de luz/shader verificado; FPS em hardware alvo ainda não medido |

## Limites

Não foram medidos FPS/GPU no hardware alvo nem realizadas inspeções em múltiplos
monitores, janela e tela cheia. Esses pontos exigem playtest manual. As capturas
não representam névoa volumétrica: usam fog exponencial regional da URP.
Não foram gerados novos assets de arquitetura; este pass melhora os materiais e
a iluminação da geometria existente. As texturas externas têm origem e licença
registradas no manifesto do asset.
