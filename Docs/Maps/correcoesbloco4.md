# Correções do Bloco 4

- [x] Revisar todas as portas e paredes garantindo que não frestas entre elas
- [x] Ajustar sala da secretaria para ficar identica a do bloco de direito
- [x] Ajustar sala de periféricos para ficar com tamanho igual as outras
- [x] Ajustar abertura de catracas para abrirem a partir do lado que o player interage
- [x] Remover as janelas que ficam ao lado do quadro branco
- [x] Aumentar altura do quadro branco
- [x] Ajustar as salas de aula para parecere um laboratório de informática
- [x] Tirar texto do painel de senha ao errar falando que o jogador deve procurar os símbolos e a senha corretar, deixar somente erro de "Senha incorreta"
- [x] Adicionar mais salas ao fim dos corredores
- [x] Revisar texturas das paredes, chão e tentar colocar outras que combinem melhor com o restante dos mapas
- [x] Melhorar a hud do painel de senha para parecer que o jogador realmente está mexendo com um painel de senha
- [x] Corrigir frestas das portas e paredes
- [x] Substituir alguns modelos por modelos da internet 3d gratuitos de melhor qualidade se existir
- [x] Fazer evento aleatório nesse mapa para luzes piscarem e apagarem por alguns segundos aleatoriamente
- [x] Recolocar a imagem personalizada que havia no meio junto com os símbolos da senha
- [x] Bug na parede do banheiro que o personagem atravessa ela
- [x] Corrigir portas do banheiro que estão com relevo para fora
- [x] Corrigir secretaria, colocar ela para depois das catracas e o balcão de atendimento deve ficar acessível pelo meio do bloco, sem corredor
- [x] Fechar parede entre banheiro masculino e feminino
- [x] Apagar sala 425 e 427 ou colocar ela ao lado oposto ao do banheiro
- [x] Virar privada, está virada para a parede
- [x] Colocar imagem que está no meio do bloco na parede
- [x] Padronizar texturas das paredes
- [x] Colocar de volta as janelas ao lado das portas que mostram a sala
- [x] Tirar altar com chave, colocar na carteira
- [x] Colocar espingarda em uma mesa tirar o texto embaixo dela
- [x] Colocar os símbolos e numeros dentro dos computadores ao invés de em cima
- [x] Encostar na parede as cadeiras e os vasos do hall, liberando o meio da circulação
- [x] Remover resquício de porta
- [x] Ajustar portas e divisórias das cabines dos banheiros, há espaço sobrando nelas
- [x] Colocar sala 428 426 e ao lado oposto do banheiro
- [x] Padronizar todas as salas, transformar elas em laboratórios
- [x] Tirar estantes e objetos que estão na frente da sala de periféricos, também tirar placa que diz "sala de cabos e periféricos", deixar  o espaço mais bagunçado
- [x] A porta da 428 e 426 foram colocadas no lado oposto do banheiro, mas não é acessível, a porta não se abre e não há janelas
- [x] Adicionar evento aleatório de luzes piscando, apagando, voltando para criar ambiente
- [x] Adicionar tags aos chãos para reproduzir som de passo
- [] Adicionar mecânica no bloco 4, quanto as luzes se apagam, os computadores que tem os símbolos trocam de monitor, então se o circulo tá no computador da 406, ele vai pra 426 por exemplo, isso deve ocorrer com pouca frequência, e  o símbolo sempre deve estar na primeira fileira dos computadores, a fileira que é próxima da janela- Fazer commit das alterações antes de começar essa tarefa




** NÃO RELACIONADAS A SALA
- [x] Preciso que os audios dos professores sejam editados, remover ruído, vozes alternativas e silencio, ajustar som para ficarem no mesmo volume
- [x] Criar sprite de chave para celas, cada chave abre 1 cela no porão,
- [x] Distribuir essas chaves entre os 3 prédios, biblioteca do campus, bloco de direito e bloco 4
- [] Corrigir hud da shotgun, arma fica com espaço para fora e imagem esticada, fazer scrip que faz com que se ajuste a cada resolução evitando que a hud da arma fique fora do lugar e fique esticada
- [] No bloco 4, a porta da manutenção deve ficar trancada, quando o player pega a ultima chave da cela e o alarme toca, a chave da manutenção spawna em uma sala aleatória, diminuir quantidade de zumbis em 20%
- [] Adicionar Créditos ao jogo, por enquanto faça o botão de créditos no menu inicial, deixe fácil de editar os textos e adicionar música




## Primeira lista — 08/11/2026 (histórico)

`[x]` indica alteração implementada e salva. Conferência por arquivos e previews fora de tela. **O jogo e o Play Mode não foram iniciados. Nenhuma janela interativa foi aberta ou trazida à frente.** Comportamentos de gameplay não receberam validação em execução.

- Portas, batentes e paredes: envelopes contínuos, folhas com folga de 2 mm por borda e vedação visual; duas colisões antigas do perímetro foram desativadas para liberar os novos vãos.
- Secretaria: módulo de Direito de 9,5 × 17 m, rotacionado para o espaço original junto à entrada. Guichê com corredor de atendimento acessível pelo hall e entrada de funcionários. Fotografias e quinta pista preservadas.
- Periféricos: divisórias internas retiradas do uso; sala 419 com área equivalente aos demais módulos internos, 13,5 × 11,75 m, estantes e diagnóstico.
- Catracas: eixo de aproximação configurado conforme a passagem, para escolher o sentido de abertura a partir do lado do jogador. Conferência do código; travessia não testada em jogo.
- Lousas: paredes traseiras fechadas, sem as janelas laterais; quadros com 1,65 m de altura e centro a 2,45 m do piso.
- Laboratórios: **86 bancadas contínuas**, cada uma com **quatro computadores**; tampo único de 7,6 × 0,95 m, cadeiras, estrutura metálica e canaleta. As mesas individuais dos alunos ficam inativas. Cada sala possui três ou quatro fileiras e uma mesa docente separada.
- Painel: erro reduzido a **“Senha incorreta”**, sem dica dos símbolos; interface com caixa metálica, parafusos, visor LCD e botões físicos. Layout/código revistos; painel não aberto em gameplay.
- Salas 425–428 acrescentadas, uma no final de cada corredor. Salas 401–424 mantidas; secretaria adicional, sem ocupar a numeração das salas.
- Acabamentos: terrazzo e reboco coerentes com LawSchool; laranja, azul e identidade do Bloco 4 conservados.
- Modelos CC0 de monitor, teclado e mouse importados. Fontes, autores, conversão e hashes em `Licenses/AssetSources/CampusBlock4/corrections-models.json`.
- Energia: evento sorteia intervalos de 45–110 s; as luzes piscam e apagam por 2–5 s, com restauração e pausa durante menus. Script e referências salvos; evento não disparado em jogo.
- Quadro central personalizado e cinco pistas mantidos; senha, chave e espingarda da 423 preservadas.

Previews: `output/Bloco4-Correcoes-*.png`. Ficha atual: [CampusBlock4.md](CampusBlock4.md). Sem Play Mode, build ou medição de FPS nesta revisão, conforme o pedido de não executar o jogo.

## Segunda lista — 08/11/2026 (histórico)

Os 12 itens novos estão implementados e salvos, incluindo o pedido acrescentado durante a revisão sobre as pistas nas telas. As decisões abaixo substituem a posição anterior da secretaria e a presença das salas 425/427 descritas no registro histórico.

- Banheiros: quatro envelopes com colisores de caixa sólidos, preservando os acessos dos corredores. Duas divisórias completas fecham o espaço entre masculino e feminino.
- Portas das cabines: retiradas as folhas inclinadas das malhas agrupadas e instaladas 16 portas alinhadas aos vãos, com dobradiça, puxadores, colisão e fechamento automático. Abertura configurada para dentro; funcionamento não executado em jogo.
- Secretaria: **substitui o laboratório 404**, conforme a última orientação do usuário. Ocupa o módulo de 13,5 × 11,75 m, depois das catracas; o guichê reutilizado de Direito abre diretamente para o hall central. A entrada de funcionários permanece pelo primeiro corredor. Antiga secretaria e corredor de atendimento ficam inativos; assentos afastados da frente do balcão.
- Salas 425/427 retiradas do uso e respectivos finais dos corredores oeste fechados. Objetos/fileIDs preservados inativos; 426/428 mantidas.
- Dezesseis vasos girados, com caixa junto à parede e bacia voltada para o interior da cabine. Alteradas apenas as cópias das malhas do Bloco 4, sem modificar os assets de origem de Direito.
- Fotografia personalizada, moldura, luminária e sequência original dos símbolos fixadas na parede do hall, sem painel isolado no meio da circulação.
- Reboco padronizado nos materiais exclusivos do cenário, preservando laranja, azul e os revestimentos próprios das áreas molhadas.
- Janelas de observação restauradas nas frentes das salas, ao lado das portas, com vidro transparente, caixilhos e colisão. Paredes junto às lousas continuam fechadas.
- Chave na bancada de alunos da 423, com sprite em escala menor; pedestal antigo inativo. Pickup e persistência originais preservados.
- Espingarda sobre a mesa docente da 423, separada do computador; suporte e texto antigos inativos.
- Cinco pistas com símbolo e número dentro das telas: quatro nos monitores originais dos laboratórios, preservando fotos e textos; cruz=5 no computador administrativo da secretaria 404. Placas azuis e presilhas anteriores inativas. Geometrias e atlas existentes reutilizados em materiais de tela compartilhados; sem modificar fotografias ou a solução do puzzle.

Restam **21 laboratórios, 75 bancadas contínuas e 300 computadores de alunos**, além dos postos docentes e administrativos. Puzzle, cinco pistas e senha preservados.

Validação: compilação/gravação pela Unity API em processo oculto, conferência de referências e previews finais em `output/Bloco4-SegundaLista-*.png`. Preservados os 35.352 IDs existentes do prefab antes desta segunda lista, os 257 da cena, importadores, fotos, definição do puzzle, scripts e assets de origem de LawSchool. **Sem jogo, Play Mode, janela interativa, build, medição de FPS ou teste de travessia/coleta em runtime.** `[x]` registra implementação salva; não afirma teste de gameplay.

## Ajuste do mobiliário do hall — 08/11/2026

Quatro longarinas e quatro vasos aproximados das faces internas das paredes, com folga geométrica de 15 mm. Bancos mantêm escala e orientação; plantas giradas para acomodar a folhagem ao longo da parede, sem atravessá-la. O vaso antes diante da secretaria foi deslocado para `z = −25,5`, junto à parede perto da entrada. Balcão, quadro/símbolos e cruzamentos dos corredores permanecem livres.

Somente oito instâncias do mobiliário do hall alteradas; cena, modelos de origem, materiais, scripts e puzzle preservados. Previews da disposição atual: `output/Bloco4-Hall-Paredes-*.png`. Conferência em prefab reaberto e renderização fora de tela; **sem jogo, Play Mode ou janela interativa**.

## Terceira lista — 08/11/2026 (estado atual)

Os cinco novos itens estão implementados e salvos:

- Entrada: desativada a barra `Secretary door closure strip` que restou sobre a porta da secretaria antiga. Parede contínua e barreira de segurança das catracas preservadas.
- Cabines: quatro conjuntos refeitos, com cinco divisórias laterais e fechamentos frontais por banheiro. Dezesseis vãos livres de 1,15 m, folhas de 1,14 m e folga de 5 mm por borda, batentes e puxadores. Eixos das dobradiças corrigidos para abertura para dentro. Vasos, pias e divisórias dos mictórios preservados; alteradas somente as malhas derivadas do Bloco 4.
- 426/428: transferidas para a fileira do outro lado dos corredores, **em frente aos banheiros**. Centro `x = 54,75`, frentes em `z = −20,25 / +20,25`, mantendo 13,5 × 17 m. Antigos acessos laterais fechados, sem estender salas para além do fim do corredor.
- Laboratórios: 424 convertida de auditório para laboratório com quatro fileiras, quatro computadores por bancada e posto docente. Demais salas de aula conferidas no mesmo padrão. **Secretaria, coordenação, copa e depósito mantidos**, conforme a confirmação do usuário. Agora são **22 laboratórios, 79 bancadas e 316 computadores de alunos**.
- Periféricos: retiradas do uso as estantes da frente, os móveis do antigo depósito compacto e a placa de cabos/periféricos. Frente livre; caixas empilhadas, cabos soltos, monitores, teclados, notebooks e ferramentas concentrados no fundo, junto à área de triagem. Assets existentes reutilizados, sem novos downloads.

Preservados os 37.492 IDs existentes antes desta lista, importadores, cena, posições dos bancos/vasos do hall, scripts, cinco pistas, fotografias, chave, espingarda e senha. Compilação, gravação/reabertura do prefab, dimensões, materiais, referências e previews em `output/Bloco4-TerceiraLista-*.png`. Nesta lista foi usado **o editor já aberto, sem foco/ativação de janela e sem iniciar jogo ou Play Mode**. Travessias, abertura das portas, coletas, build, FPS e NavMesh não foram executados. Ferramentas temporárias desta tarefa retiradas após verificar os resultados persistentes.

## Quarta lista — 08/11/2026

Seis itens implementados e salvos nesta rodada. A nova troca de monitores durante apagões, a adaptação da HUD da espingarda, a progressão da manutenção e os créditos continuam pendentes. O checkpoint Git é anterior à implementação dessas novas tarefas.

- **426/428:** as portas e janelas já existiam, mas dois fechamentos antigos de corredor cobriam toda a frente. Esses fechamentos agora ocupam somente `x = 61,5…71,75`, liberando as frentes `x = 48…61,5`. Preservados objetos, IDs, materiais, folhas e janelas; volumes de aproximação das portas ajustados.
- **Energia:** primeira falha em 12–24 s, próximas em 35–75 s e apagão de 2–4 s, com piscadas e retorno. 199 luzes e 182 difusores vinculados, iluminação residual pontual e pausa nos menus. As pistas permanecem nos monitores atuais; a nova mecânica de redistribuição ainda não foi implementada.
- **Passos:** 129 objetos de piso com colisão ativa receberam tag `Eco` e `FootstepSurface` do tipo `Interior`, reutilizando o banco existente do player.
- **Chaves:** oito sprites numerados, cada chave vinculada somente à respectiva cela; três em Direito, três na Biblioteca e duas no Bloco 4. A chave do alarme de Direito continua sendo a cela 1; a chave original do puzzle da 423 passa a abrir a cela 7. Senha **24153**, cinco pistas, fotografias, espingarda e funcionamento original do puzzle preservados. Contrato: [PrisonCellKeys.md](PrisonCellKeys.md).
- **Áudios:** onze arquivos existentes tratados com redução moderada de ruído, corte de silêncio e normalização próxima de −18 LUFS. Henrique: removido apenas o “calma”, mantendo “Vai Corinthians”; Bianca: mantida somente a comemoração final “yipeee”, conforme respostas do usuário. GUIDs, importadores e associações existentes preservados. Medições e hashes: `Licenses/AssetSources/Teachers/audio-edit.json`.

Validação em modo de edição: controle negativo com porta fechada e cápsula do player livre em cinco posições por vão com a folha posada aberta; matriz 8 × 8 de chaves/celas, duplicatas e IDs inválidos; referências, IDs e hashes; análise dos áudios e previews fora de tela em `output/Campus-Ultimas-*.png`. As quatro cenas, os importadores anteriores, fotografias e definição do puzzle permaneceram byte a byte iguais nesta rodada. **Sem iniciar jogo/Play Mode, abrir outro editor ou ativar/focar a janela da Unity.** Abertura automática, coleta, evento aleatório, escuta no jogo, transições, build e FPS não foram testados em runtime. Ferramentas temporárias desta rodada removidas após conferir os assets persistentes.
