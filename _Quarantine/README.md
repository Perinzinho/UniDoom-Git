# Quarentena do projeto

Os arquivos aqui estão fora de `Assets/` e não são importados pela Unity nem incluídos automaticamente no build. Nada foi excluído definitivamente.

Em `2026-10-15/Assets/` estão os quatro geradores de mapas arquivados por escolha explícita do usuário e 30 meshes sem consumidores, comprovadamente equivalentes a meshes mantidas. Cada arquivo acompanha seu `.meta` original.

Em `2026-10-15/EmptyFolders/` estão 32 pastas que ficaram vazias após a organização, com os `.meta` de pasta. Os nomes numerados evitam colisões entre pastas de mesmo nome; o caminho original está registrado no journal.

Registros completos, com nome/caminho original, destino, data, motivo, dependências verificadas e mesh equivalente mantida:

- `../Tools/ProjectOrganization/quarantine-journal.json`
- `../Tools/ProjectOrganization/empty-folder-journal.json`
- `../Tools/ProjectOrganization/path-map.json`

Para restaurar toda a organização, consulte `../Tools/ProjectOrganization/rollback.py`. Para recuperar um arquivo individual, consulte o mapa de caminhos e restaure arquivo e meta juntos com a Unity fechada, ou utilize a API de assets no Editor; confira primeiro se não há GUID ou caminho de destino em conflito.

Não exclua definitivamente esta pasta sem autorização do usuário. As cópias em Tools/TotalChaosSkill/Source são fontes históricas e não substituem a preservação dos `.meta` originais aqui armazenados.
