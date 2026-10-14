# Validação do refinamento da arena — 14/10/2026

Unity 6000.5.7f1, URP 17.5, projeto isolado `.utmp/BasementPreview`.

## Prefab e gameplay

O estágio final preservou os 1.850 IDs antigos de Basement e os 125 IDs de
BasementFinalArena. A comparação estrutural mantém os blocos antigos de malhas,
colliders, rigidbodies, CharacterController, instâncias aninhadas e scripts de
gameplay. Transformações antigas preservadas; apenas novos filhos decorativos.

Os testes reais de CharacterController passaram no prefab e na cena: entrada,
arena, portão, corredor e sala final. Verificadas as portas em proximidade,
bloqueio e ambos os sentidos de abertura. A simulação do encontro passou:
12 inimigos, portão fechado com um sobrevivente e abertura após a última morte.

Compilação e renderização reais concluídas sem erros C# ou Render Graph;
compatibilidade do shader com SRP Batcher aprovada. Há avisos de APIs obsoletas
já usadas pelas ferramentas do projeto.

## Visual e orçamento

- Corpo da maca e torsos no piso usam a mesma malha anatômica MakeHuman, UVs e
  textura de pele. As cabeças, braços e pernas removidos foram espalhados junto
  aos torsos, com tampas de ferida e sangue.
- LOD próximo: 26.756 triângulos de corpo e 25.496 de tecido. LOD distante:
  8.061 e 2.896. São duas malhas por nível, com troca por altura na tela.
- Decoração ambiental consolidada em nove renderizadores por material;
  elementos da mesa também consolidados. Os torsos e fragmentos formam três
  renderizadores adicionais por material. Nenhum collider novo na decoração.
- Três luminárias defeituosas. As fontes principais restantes permanecem
  estáveis. Sem novas luzes de sombra; cubemap da arena estático em 128 px.
- Perfil da arena recarregado e conferido: ajustes de cor e override de
  exposição persistidos. Grain 0,035, bloom 0,05, vinheta 0,12.
- Capturas reais com as mesmas câmeras do pass anterior: arena de luminância
  média 0,1220 para 0,1053, redução de aproximadamente 13,7%. Pixels abaixo
  de 0,025: 4,94%. Não são medições de FPS.
- Capturas de boss, corredor e lanterna permanecem próximas das métricas do
  pass anterior; pequenas diferenças vieram da cena atualizada externamente.

`capture-metrics.txt` e `Horror-*.png` registram as capturas finais, incluindo
`Horror-Remains.png`, com os torsos e membros soltos no ambiente da arena.

## Execução

O verificador de Play Mode percorre os quatro volumes regionais, confere névoa,
pós-processamento e lanterna e verifica a restauração do ambiente ao sair.
Também mede a variação de intensidade das três lâmpadas e compara a emissão
de todos os renderizadores luminosos com a intensidade de cada luz.

Resultados em `runtime-validation.txt` e `arena-flicker-validation.txt`.
Logs completos em `.utmp/Basement-arena-horror-final.log` e
`.utmp/Basement-arena-horror-play.log`. O instalador exige os marcadores de
aprovação dos dois logs antes de aplicar os assets ao projeto principal.

A cena principal foi salva externamente durante o trabalho com componentes
adicionais de luz URP. Sua versão atual foi copiada para o projeto isolado e os
testes finais e de Play Mode foram repetidos com sucesso. O instalador preservou
essa cena e verificou sua igualdade com a versão usada nos testes. Os prefabs,
dependências finais e respectivos metas instalados foram comparados byte a byte
com o projeto testado; hashes dos dados originais MakeHuman conferidos.

## Revisão pela skill

Avaliação artística das capturas, com verificação técnica: **21/24**.

| Critério | Nota | Evidência ou limite |
|---|---:|---|
| Escala e arquitetura | 2 | Dimensões existentes e percurso preservados |
| Materiais distinguíveis | 2 | Pele, tecido, metal, concreto e borracha |
| Desgaste causal | 2 | Umidade junto às bombas, escorrimentos e rastros |
| Fontes e direção da luz | 2 | Luminárias físicas e luz sobre a mesa |
| Sombras e navegação | 2 | Arena mais escura com rota ainda legível |
| Profundidade e composição | 2 | Pilares, maca caída e focos laterais |
| Uso anterior e abandono | 2 | Instrumentos, maca, soro, cortina e resíduos |
| Contato e colisão dos props | 1 | Passagem validada; tecido é uma aproximação estática |
| Legibilidade dos detalhes | 1 | Corpo detalhado; sinalização antiga tem limitações |
| Identidade própria | 2 | Instalação subterrânea com abandono mortuário |
| Pós-processamento | 2 | Valores discretos, perfil persistido |
| Desempenho | 1 | LOD e orçamento verificados; sem medição no hardware alvo |

Não foram realizados testes em múltiplos monitores, janela/tela cheia nem
profiling de FPS/GPU no hardware alvo. O teste visual suspende movimento e
combate; estes são verificados separadamente pelos testes acima.
