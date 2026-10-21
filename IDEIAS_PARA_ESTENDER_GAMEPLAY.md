# Ideias para estender a gameplay do UniDoom

## Objetivo

Aumentar o tempo em que o jogador explora, decide, luta e descobre algo relevante. Cada trecho acrescentado deve oferecer uma situação própria, uma recompensa ou uma mudança perceptível no mundo.

Este documento propõe conteúdo; não representa funcionalidades já implementadas. As sugestões aproveitam o campus e seus setores, a biblioteca, as salas de aula e o porão com pré-arena, arena, boss e passagem para a prisão. A sala do diretor fica fora desta proposta enquanto estiver em desenvolvimento.

A duração atual da campanha ainda precisa ser medida. Os tempos abaixo são hipóteses para protótipos, não estimativas garantidas nem valores para somar ao tempo atual.

## O que aproveitar de Total Chaos

O Total Chaos clássico, mod de Doom II, combina exploração de ambientes abandonados, descoberta da história por cartas, recursos escassos e inimigos com padrões de ataque distintos. Sua descrição oficial destaca a passagem por locais com funções diferentes e o aprendizado necessário para sobreviver. [Referência: página do mod no ModDB](https://www.moddb.com/mods/total-chaos).

Para o UniDoom, a proposta é adaptar esses princípios ao combate com armas de fogo que o jogo já tem: espaços com identidade, decisões sobre recursos, ameaça legível e descobertas que justificam continuar. A arquitetura escolar e sua transformação até o porão podem sustentar essa progressão.

Não é necessário implantar todo um sistema de sobrevivência para conseguir isso. Fome, sede, fabricação e inventário complexo aumentariam bastante o escopo; não são a primeira escolha para esta expansão.

## Bases que o projeto já oferece

| Base existente | Possibilidade de expansão |
| --- | --- |
| Chaves e checkpoints na coleta | Pequenos objetivos com consequência visível e retorno encurtado |
| Encontros por área e após coleta na biblioteca | Retornos com nova situação e encontros opcionais |
| Apagão associado à progressão da biblioteca | Uma mudança de ambiente com função na navegação |
| Encontro no átrio do Cylinder | Um espaço central que responde ao avanço da campanha |
| Pistola, escopeta, munição e recarga | Escolhas de rota e preparação antes de um confronto |
| Pré-arena, arena e boss do porão | Escalada de situações sem interromper a travessia interna |
| Passagem para a prisão após o boss | Recompensa espacial e narrativa para o clímax |

Os atalhos do menu e a munição temporária abundante no porão são ferramentas de teste. O balanceamento proposto aqui precisa de uma sessão com recursos representativos da campanha.

## 1. Desvios opcionais com recompensa visível

**Onde:** biblioteca, salas de aula e entorno do átrio. **Prioridade:** alta. **Esforço relativo:** baixo a médio, conforme o espaço disponível.

O jogador vê munição ou outro recurso útil através de uma grade ou abertura. A entrada fica por uma sala lateral próxima, onde um encontro curto protege o acesso. Ao sair, uma porta abre de volta para o caminho principal.

O ciclo é: enxergar a recompensa → avaliar o risco → entrar → resolver o encontro → coletar → voltar por um atalho. O objetivo principal continua acessível para quem prefere seguir.

- Dar pistas do tipo de recompensa antes da decisão.
- Manter parte do ganho acessível mesmo quando a luta custa recursos.
- Ajustar o custo para que um jogador razoavelmente preciso tenha benefício.
- Evitar uma sequência de salas opcionais idênticas com a mesma emboscada.

**Hipótese de duração:** 1–3 minutos por desvio escolhido. Começar com um único desvio bem resolvido na biblioteca.

## 2. Retorno da biblioteca com uma situação diferente

**Onde:** trajeto após obter a chave da biblioteca. **Prioridade:** alta. **Esforço relativo:** médio.

A coleta já oferece um ponto natural para mudar a situação. Proposta: durante o apagão, o som de uma grade e uma luz de emergência revelam uma saída lateral. Um encontro curto pressiona a escolha entre atravessar a área conhecida ou usar a nova conexão.

A saída deve ser reconhecível também sem áudio. O caminho pode ser apresentado anteriormente, ainda fechado, para que sua abertura tenha significado.

O retorno ganha uma decisão e uma nova composição de combate. O trecho anterior não precisa ser repovoado inteiro. Os inimigos entram por acessos plausíveis, longe do corpo do jogador e de seu campo de visão imediato.

**Reutilização:** gatilhos e apagão do `LibraryEncounterDirector`. A abertura do atalho e sua persistência precisariam ser implementadas e verificadas com checkpoints.

## 3. Campus que responde às conquistas

**Onde:** átrio do Cylinder e conexões entre setores. **Prioridade:** média. **Esforço relativo:** médio.

Usar poucos estados do espaço central, associados a marcos reais da campanha:

1. Na primeira passagem, apresentar as saídas e um bloqueio reconhecível.
2. Após uma conquista, abrir uma conexão curta e deslocar a atenção para outro setor com luz, som e enquadramento.
3. Perto da descida ao porão, introduzir um confronto específico e sinais mais fortes da transformação do prédio.

Cada estado precisa mudar uma ação possível. Apenas trocar luzes teria valor atmosférico, mas acrescentaria pouca gameplay.

Não colocar inimigos novos em toda visita. Depois que o jogador domina uma rota, deixar que use esse conhecimento para atravessá-la mais rápido. Persistir os estados para evitar repetir recompensas e encontros após morrer ou trocar de cena.

## 4. Um objetivo físico curto em uma sala de aula

**Onde:** uma sala e seu corredor imediato. **Prioridade:** média. **Esforço relativo:** médio; exige interação nova.

Exemplo: uma porta de serviço está sem energia. O quadro elétrico e a porta ficam próximos e podem ser relacionados por cabos e sinalização do próprio prédio. O jogador acessa o quadro por dentro da sala, aciona a alimentação e vê a porta responder.

Na volta, um encontro muda o uso do espaço: as fileiras de mesas criam duas rotas, e a saída recém-aberta permite reposicionamento.

- Mostrar causa e efeito no mesmo trecho sempre que possível.
- Usar uma ou duas ações compreensíveis.
- Evitar procurar fusíveis minúsculos pelo mapa inteiro.
- Não exigir texto explicativo na tela para entender a abertura.

**Hipótese de duração:** 2–4 minutos com exploração e combate. Fazer primeiro uma versão com um acionamento; adicionar etapas apenas se trouxerem uma decisão diferente.

## 5. Preparação com escolha antes da arena

**Onde:** porão pré-arena. **Prioridade:** alta. **Esforço relativo:** baixo a médio.

Antes de entrar, o jogador consegue observar parte da arena e percebe dois pequenos pontos de preparação. Um depósito contém munição, com ameaça visível ou audível. Outro acesso oferece uma travessia mais segura e recursos menores.

A rota principal deve oferecer recursos suficientes para uma tentativa viável. O desvio melhora a margem de erro, sem se tornar uma obrigação escondida.

O acesso à arena continua fluido, sem tela de carregamento entre esses espaços. A preparação ocorre enquanto o jogador se movimenta, examina o local e decide o que buscar.

**Hipótese de duração:** 1–3 minutos opcionais. Evitar uma lista de tarefas antes de cada tentativa do boss.

## 6. Arena com mudanças táticas

**Onde:** arena do porão. **Prioridade:** alta. **Esforço relativo:** médio.

Ampliar a variedade do confronto com poucos momentos distintos:

| Momento proposto | Decisão oferecida ao jogador |
| --- | --- |
| Entrada de um grupo por um acesso reconhecível | Onde se posicionar e qual alvo priorizar |
| Abertura de uma conexão lateral | Usar a nova circulação ou manter a posição conhecida |
| Recursos em um ponto exposto | Quando abandonar a posição para reabastecer |

As entradas de inimigos devem ser anunciadas e os acessos precisam parecer utilizáveis por eles. Os intervalos servem para recarregar, perceber o ambiente e mudar de posição; encerrar pausas quando sua função já estiver cumprida.

Evitar alongar a luta repetindo a mesma onda ou aumentando a vida de todos. A arena deve terminar enquanto ainda tem algo a oferecer. Novos acessos exigem validar colisão, navegação e possibilidade de o jogador ficar preso.

## 7. Situações de combate que aproveitam as armas existentes

**Onde:** encontros selecionados de diferentes setores. **Prioridade:** média. **Esforço relativo:** baixo para composição; maior para IA nova.

Antes de criar novos inimigos, variar a relação entre distância, quantidade, cobertura e saídas. Uma ameaça isolada em corredor permite precisão; um grupo anunciado chegando por uma porta cria uma oportunidade diferente para a escopeta; inimigos separados por pilares exigem reposicionamento.

Cada encontro deve ter uma diferença que o jogador percebe em movimento. Se todos forem resolvidos recuando pelo mesmo corredor, revisar a disposição e as conexões do ambiente.

Comportamentos novos, como investida com preparação visível ou ataque à distância, podem ser uma etapa posterior. Precisam de animação, áudio e tempo de reação próprios. Variações de aparência existentes não devem ser tratadas como comportamentos já implementados.

## 8. Micro-histórias que conduzem a uma descoberta

**Onde:** biblioteca, salas, manutenção e prisão. **Prioridade:** média. **Esforço relativo:** baixo a médio.

Construir pequenas sequências que o jogador entende enquanto explora. Exemplo proposto: carteiras foram usadas para bloquear uma porta; marcas no piso seguem até um acesso de serviço; do outro lado há um posto improvisado com recursos e evidências de quem tentou se proteger.

A descoberta recompensa a atenção e pode revelar um desvio útil. Textos curtos acrescentam contexto para quem quiser ler, sem carregar instruções obrigatórias de progressão.

Aplicar a direção visual de Total Chaos: objetos com função anterior clara, desgaste com causa, luz plausível e um foco principal por composição. Uma pista importante precisa aparecer à distância normal de jogo; não depender de vasculhar cada centímetro de uma sala escura.

## 9. Prisão como recompensa jogável do boss

**Onde:** passagem e prisão reveladas após a morte do boss. **Prioridade:** alta para a leitura da abertura; média para expandir o conteúdo. **Esforço relativo:** médio.

O jogador precisa perceber que o mundo mudou: ruído mecânico localizado, movimento visível do fechamento e luz revelando o vão completamente desobstruído. O enquadramento da arena deve permitir reconhecer essa mudança ao retomar o controle da situação.

A prisão pode oferecer uma sequência curta: atravessar a passagem, reconhecer uma evidência importante do que aconteceu no prédio e alcançar um destino ou saída claramente apresentado. Uma interação final pode dar uma conclusão à descoberta, se combinar com a história planejada.

O conteúdo narrativo exato ainda deve ser definido. Não presumir aqui a existência de um prisioneiro, resgate ou final específico.

**Hipótese de duração:** 2–4 minutos. Preservar o peso do boss e usar esse trecho como consequência do clímax. Uma segunda arena extensa logo em seguida enfraqueceria essa função.

## 10. Rejogabilidade com encontros alternativos controlados

**Onde:** poucos encontros opcionais. **Prioridade:** baixa, depois da primeira campanha funcionar bem. **Esforço relativo:** médio a alto.

Preparar duas composições equivalentes para uma sala e escolher uma delas no início da partida. A variação muda posicionamento ou composição do encontro, mantendo objetivo, acesso e recursos essenciais previsíveis.

Salvar a escolha da variante para que morrer não altere as regras do trecho. As duas versões precisam ser testadas com os mesmos critérios de dificuldade e recompensa.

Isso aumenta o interesse de uma segunda partida. Não deve ser contabilizado como aumento da duração da primeira campanha.

## Primeira expansão recomendada

Começar com uma pequena sequência completa na biblioteca:

1. Apresentar uma recompensa visível em um desvio opcional.
2. Colocar um encontro curto com duas possibilidades de circulação.
3. Fazer o desvio desembocar em um atalho para a rota principal.
4. Após a coleta da chave, usar o apagão existente para revelar uma mudança legível no retorno.
5. Confirmar que checkpoint e reentrada preservam corretamente o progresso.

Essa sequência permite testar exploração, combate, recompensa e orientação com os sistemas que o projeto já oferece. Só expandir o modelo para outros setores depois de observar uma pessoa jogando sem instruções.

Em seguida, priorizar a preparação do porão, uma mudança tática na arena e a recompensa da prisão. Objetivos elétricos e novas IAs ficam para uma etapa posterior, pois exigem sistemas e validação adicionais.

## Ritmo proposto para uma sessão

Uma sequência possível é: observar o próximo objetivo → escolher um desvio → resolver um encontro → receber a recompensa → usar uma conexão curta → descobrir uma consequência da progressão.

Alternar intensidade de acordo com a situação. Um trecho silencioso pode ser valioso quando o jogador está reconhecendo uma ameaça ou interpretando o lugar. Depois de um combate, deixar espaço para recarregar e decidir o próximo passo. Evitar disparar outra onda automaticamente em toda coleta.

As faixas de tempo deste documento incluem atividades que podem se sobrepor. Não somá-las para prometer uma duração final. Primeiro medir a campanha atual; depois comparar uma versão com a expansão e observar onde os minutos novos foram gastos.

## O que evitar para aumentar duração

- Inimigos com vida maior sem mudança na forma de enfrentá-los.
- Ondas repetidas, respawn infinito e salas que só terminam após uma espera fixa.
- Caminhadas longas de volta por locais já resolvidos, sem descoberta ou decisão.
- Chaves em sequência para todas as portas e objetos essenciais difíceis de enxergar.
- Falta de munição que obriga a procurar recursos sem pista de onde encontrá-los.
- Texto obrigatório longo, animação de interação demorada e interrupções frequentes.
- Escuridão que esconde a navegação e obstáculos decorativos bloqueando uma passagem aberta.
- Morte que exige repetir preparação e deslocamento extensos antes de tentar o desafio novamente.
- Introduzir fabricação, fome ou inventário complexo apenas para ocupar tempo.

## Como decidir se a expansão funcionou

Observar uma primeira partida de alguém que não conheça o mapa. Registrar, por trecho, tempo total, atividade predominante, mortes, recursos antes/depois e momentos em que a pessoa não soube o que fazer.

| Pergunta | Evidência útil |
| --- | --- |
| O tempo novo trouxe decisões? | O jogador considerou uma rota, um recurso ou um posicionamento |
| O desvio valeu o esforço? | A recompensa teve utilidade compatível com o risco e o gasto |
| O objetivo ficou claro? | A pessoa identificou a continuação pelas pistas do ambiente |
| O retorno teve função? | Houve uma situação nova e acesso mais eficiente ao destino |
| O combate variou? | A pessoa mudou de posição, prioridade ou uso de arma |
| A morte permitiu aprender? | A nova tentativa chegou rapidamente ao desafio relevante |
| A abertura da prisão foi percebida? | A pessoa encontrou e atravessou a passagem sem explicação externa |

Se o tempo aumentou principalmente por desorientação, busca repetitiva ou repetição após morte, revisar a sequência antes de acrescentar mais conteúdo.

Também verificar colisões, acerto das balas, pontos de nascimento e travessia nas áreas alteradas. Essas falhas interferem diretamente na leitura dos testes de ritmo.

## Referências do projeto

Pontos de partida para uma futura implementação, sem pressupor que os sistemas novos estejam prontos:

- [Chaves e checkpoints](Assets/_Project/Global/Scripts/Player/PlayerPickupKey.cs).
- [Encontros e apagão da biblioteca](Assets/_Project/Scenes/Library/Scripts/EncounterRevision/LibraryEncounterDirector.cs).
- [Encontro do átrio](Assets/_Project/Scenes/Cylinder/Scripts/CylinderEnemySpawns.cs).
- [Encontro da arena do porão](Assets/_Project/Scenes/Basement/Scripts/BasementArenaEncounter.cs).
- [Encontro do boss](Assets/_Project/Scenes/Basement/Scripts/DirectorBossEncounter.cs).
- [Passagem para a prisão](Assets/_Project/Scenes/Basement/Scripts/BasementPrisonPassage.cs).

Documento de propostas. Nenhuma alteração de gameplay, cena ou balanceamento foi feita para sua elaboração.
