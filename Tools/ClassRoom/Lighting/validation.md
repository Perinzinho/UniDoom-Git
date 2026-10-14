# Validação executada — ClassRoom

Executada em 13/10/2026 com Unity 6000.5.7f1 / URP 17.5 na cópia isolada
`.utmp/BasementPreview`. Os assets validados foram instalados no projeto principal.

| Verificação | Resultado observado |
|---|---|
| Compilação Unity e carregamento do prefab externo | Passaram; materiais, malhas, Volume e reflexão válidos |
| Estabilidade em Play Mode | 120 frames renderizados em corredor, frente e porta; variação de intensidade igual a zero; exposição 0 EV constante |
| Transição pela entrada | 82 amostras nos dois sentidos; névoa e sol coincidiram com a interpolação espacial |
| Atlas de sombras no ClassRoom | Zero reduções automáticas durante as posições e transições testadas |
| Restauração entre áreas | As seis regiões existentes selecionaram seus parâmetros; saída restaurou névoa, ambiente, câmera, sol e tiers de sombras |
| CharacterController real | Rotas de 186 e 83 pontos, ida e volta, no prefab e na SampleScene |
| Porta e circulação | Abertura nos dois sentidos, passagem do player, corredores e cápsula de 1 m passaram |
| Integridade | 2.986 IDs anteriores preservados; 40 blocos de iluminação alterados; nenhum bloco de suporte novo no prefab |
| Escopo | Cena principal, player, prefab externo ClassRoom, outros mapas, geometria e colisores preservados; materiais exclusivos da sala atualizados |

Na captura do corredor **sem lanterna**, a proporção de pixels com luminância
RGB abaixo de 0,02 caiu de **48,92% para 3,66%**; a mediana passou de 0,0224
para 0,0619. O clipping máximo das quatro vistas finais ficou em 0,05%.
São medidas das capturas em RGB de exibição, não lux nem validação de monitor.
Veja [capture-metrics.txt](capture-metrics.txt) e [runtime-validation.txt](runtime-validation.txt).

Os renderizadores permanecem em 607 e nenhuma luz foi adicionada ao cenário.
Há três fontes da sala com sombras, além da lanterna, e reflexão estática 128².
O custo de texturas aumenta ao recuperar os 512 px: seis conjuntos PBR reutilizados
com importações exclusivas da sala, mais o mosaico original filtrado. O orçamento
de atlas foi testado; FPS, GPU timings e desempenho no hardware alvo não foram medidos.

O teste de atmosfera desabilita scripts de gameplay. A circulação usa o
CharacterController real em modo de edição; combate, ritmo de exploração,
alternância física da tecla F, tela cheia e monitores distintos requerem playtest.
A revisão conserva os danos e props existentes; não faz bake de iluminação indireta.
Não foi atribuída uma nota de aprovação ao checklist da skill sem esses testes.

Logs da execução:

- `.utmp/ClassRoom-lighting-build4.log`: geração, primeiras capturas e verificação.
- `.utmp/ClassRoom-lighting-final.log`: prefab instalado e capturas finais.
- `.utmp/ClassRoom-lighting-play.log`: Play Mode e restauração das seis regiões.
- `.utmp/ClassRoom-lighting-gameplay.log`: rotas, porta e herança na cena.

As primeiras tentativas registraram acesso bloqueado à licença no sandbox,
uma API obsoleta da Unity e dois ajustes necessários no verificador. Esses
problemas foram corrigidos antes dos testes finais; não são resultados aprovados.
A URP ainda registra redução do atlas ao renderizar outras áreas com seus
parâmetros anteriores; a reserva do ClassRoom não redesenha a iluminação desses mapas.
