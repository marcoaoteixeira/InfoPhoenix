# InfoPhoenix :bird:

Esse é o repositório de código do InfoPhoenix, uma nova implementação do antigo
[InfoFenix](https://github.com/marcoaoteixeira/InfoFenix).

![develop](https://github.com/marcoaoteixeira/InfoPhoenix/actions/workflows/develop.yml/badge.svg)
![publish](https://github.com/marcoaoteixeira/InfoPhoenix/actions/workflows/publish.yml/badge.svg)

## Release Atual :floppy_disk:

* [v2.0.0](https://github.com/marcoaoteixeira/InfoPhoenix/releases/tag/v2.0.0)

## Tecnologia :computer:

A grande diferença dessa nova implementação é o runtime utilizado. Na
implementação anterior utilizamos o .NET Framework 4.5, porém a Microsoft já
deixou de dar suporte para essa versão a algum tempo e pensando na
manutenibilidade da solução, optou-se por migrar esse projeto para o
.NET Core 10.

Além disso, foi integrado ao projeto aa biblioteca
[WPF-UI](https://github.com/lepoco/wpfui) para interface mais fluída e
amigável.

Principais tecnologias utilizadas:

* C# (.NET 10, `net10.0-windows`)
* WPF + [WPF-UI](https://github.com/lepoco/wpfui)
* [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) para MVVM
* [Nameless.Windows](https://github.com/marcoaoteixeira/Nameless) (pacote privado) para hosting, DI, configuração e busca
* SQLite (via EF Core) como banco de dados relacional
* Lucene para busca textual (via `Nameless.Windows`)
* xUnit para testes

O projeto principal (`Nameless.InfoPhoenix.Client`) é baseado na amostra
[Nameless.WinApp](https://github.com/marcoaoteixeira/Nameless/tree/main/samples/windows/Nameless.WinApp)
do repositório Nameless.

## Contribuições :raising_hand:

* @marcoaoteixeira

## Como Abrir Um Chamado :ledger:

1. Para abrir um novo "chamado", vá na aba "Issues", clique no botão
"New Issue" e preencha o formuário.

2. Não esqueça de adicionar a qual categoria essa "issue" pertence, utilizando
o menu "Labels" a sua direita, basta clicar na engrenagem.

3. Caso seja um possível "bug", forneça algum exemplo de como reproduzir o
comportamente, screenshot e o log do sistema.