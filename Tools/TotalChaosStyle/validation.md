# Resultado da validação instalada

- Unity 6000.5.7f1: quatro processos de validação concluídos sem erros de
  compilação, exceções ou erros de Render Graph.
- Geometria e materiais: cinco prefabs carregados e renderizados na posição
  real do mapa; 789 sólidos receberam chanfros. As 46 folhas do cilindro usam
  o mesmo tamanho de 1,85 × 3,25 m e o mesmo desenho.
- Sala: CharacterController real percorreu as rotas de ida e volta no prefab
  e na SampleScene; abertura da porta e escala do mobiliário universitário
  verificadas.
- Porão: paredes e teto fechados; circulação pela arena, comporta, corredor
  e sala do chefão verificada no prefab e no mapa. Comporta fechada bloqueia.
  Portas externas abrem nos dois sentidos. Sem invasão das salas vizinhas.
- Cilindro: dois circuitos, entrada e três portas testados com o player real
  no prefab e no mapa; portas fechadas bloqueiam, abertas permitem passagem.
  43 portas decorativas sem interação, X ou maçaneta; 80 objetos apoiados nas
  bancadas e cobertura do teto verificada por 384 raios.
- Instalação: os cinco prefabs têm bytes idênticos às versões preparadas e
  verificadas. Todos os 7.309 blocos anteriores foram conservados, com
  transforms, colisores, scripts e pais da hierarquia intactos.
- Todas as 1.426 referências de GUID nos prefabs e materiais resolvem no
  projeto principal. Foram instaladas 1.082 malhas, 65 materiais e 24 texturas
  próprias referenciadas pela revisão.
- A cena principal, o player, o prefab externo da sala e toda a arte anterior
  fora dos cinco prefabs autorizados permanecem idênticos ao snapshot.

Os testes rodam em modo de edição. Combate, desempenho em hardware alvo e ritmo
de exploração dependem de playtest. As capturas ficam nesta pasta com o prefixo
`TotalChaos-` e o estudo visual está em [README.md](README.md).
