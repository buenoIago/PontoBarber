using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;

namespace PontoBarber.Aplicacao.Modulos.Profissionais;

/// <summary>
/// Disponibilidade semanal individual (Task 6.2). O proprietário pode editar
/// qualquer profissional da barbearia; o barbeiro, somente a própria janela.
/// </summary>
public sealed class GestaoDisponibilidadeSemanal(
    IDisponibilidadeSemanalRepositorio repositorio,
    IHorarioFuncionamentoRepositorio horarios,
    IProfissionalRepositorio profissionais)
{
    public async Task<IReadOnlyList<DisponibilidadeSemanalResponse>> ListarAsync(
        Guid barbeariaId,
        Guid profissionalId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirPermissaoAsync(barbeariaId, profissionalId, usuarioId, cancellationToken);

        var janelas = await repositorio.ListarAsync(barbeariaId, profissionalId, cancellationToken);
        return janelas.OrderBy(janela => (int)janela.DiaSemana).Select(ParaResponse).ToList();
    }

    public async Task<IReadOnlyList<DisponibilidadeSemanalResponse>> SalvarAsync(
        Guid barbeariaId,
        Guid profissionalId,
        IReadOnlyList<JanelaDisponibilidadeRequest> request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirPermissaoAsync(barbeariaId, profissionalId, usuarioId, cancellationToken);

        ValidarJanelas(request, profissionalId, barbeariaId);

        var funcionamento = await horarios.ListarPorBarbeariaAsync(barbeariaId, cancellationToken);
        foreach (var janela in request)
        {
            var dia = funcionamento.FirstOrDefault(item => item.DiaSemana == (DiaSemana)janela.DiaSemana);
            if (dia is null || dia.Fechado || dia.AbreAs is null || dia.FechaAs is null ||
                janela.IniciaEm < dia.AbreAs || janela.TerminaEm > dia.FechaAs)
                throw new ExcecaoHorarioAplicacao(
                    CodigosErro.AgendaForaFuncionamento,
                    "A janela de disponibilidade está fora do funcionamento da barbearia.");
        }

        var existentes = await repositorio.ListarAsync(barbeariaId, profissionalId, cancellationToken);
        var novas = new List<DisponibilidadeSemanal>();
        var diasSolicitados = request.Select(janela => (DiaSemana)janela.DiaSemana).ToHashSet();

        foreach (var janela in request)
        {
            var diaSemana = (DiaSemana)janela.DiaSemana;
            var existente = existentes.FirstOrDefault(item => item.DiaSemana == diaSemana);
            if (existente is not null)
                existente.AtualizarHorario(janela.IniciaEm, janela.TerminaEm);
            else
                novas.Add(DisponibilidadeSemanal.Criar(
                    barbeariaId, profissionalId, diaSemana, janela.IniciaEm, janela.TerminaEm));
        }

        var removidas = existentes.Where(item => !diasSolicitados.Contains(item.DiaSemana)).ToList();
        if (removidas.Count > 0)
            await repositorio.RemoverAsync(removidas, cancellationToken);
        if (novas.Count > 0)
            await repositorio.AdicionarAsync(novas, cancellationToken);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);

        var salvas = await repositorio.ListarAsync(barbeariaId, profissionalId, cancellationToken);
        return salvas.OrderBy(janela => (int)janela.DiaSemana).Select(ParaResponse).ToList();
    }

    private static void ValidarJanelas(
        IReadOnlyList<JanelaDisponibilidadeRequest> request,
        Guid profissionalId,
        Guid barbeariaId)
    {
        if (request.Select(janela => janela.DiaSemana).Distinct().Count() != request.Count ||
            request.Any(janela => !Enum.IsDefined(typeof(DiaSemana), janela.DiaSemana)))
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["dias"] = ["Informe no máximo uma janela por dia da semana."]
            });

        foreach (var janela in request)
        {
            var erros = DisponibilidadeSemanal.ValidarDados(
                barbeariaId, profissionalId, (DiaSemana)janela.DiaSemana, janela.IniciaEm, janela.TerminaEm);
            if (erros.Count > 0)
                throw new ExcecaoValidacaoAplicacao(
                    erros.ToDictionary(erro => erro.Campo, erro => new[] { erro.Mensagem }));
        }
    }

    private async Task ExigirPermissaoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var (perfilUsuario, vinculoUsuario) = await ObterVinculoAtivoAsync(
            barbeariaId, usuarioId, cancellationToken);

        var vinculoAlvo = await profissionais.ObterVinculoAsync(barbeariaId, profissionalId, cancellationToken);
        if (vinculoAlvo is null || vinculoAlvo.Situacao == StatusVinculo.Removido)
            throw AcessoNegado();

        if (vinculoUsuario.Cargo != CargoProfissional.Proprietario && perfilUsuario.Id != profissionalId)
            throw AcessoNegado("Um profissional só pode gerenciar a própria disponibilidade.");
    }

    private async Task<(PerfilProfissional Perfil, VinculoProfissional Vinculo)> ObterVinculoAtivoAsync(
        Guid barbeariaId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(usuarioId, out _))
            throw AcessoNegado();

        var perfil = await profissionais.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        if (perfil is null)
            throw AcessoNegado();

        var vinculo = await profissionais.ObterVinculoAsync(barbeariaId, perfil.Id, cancellationToken);
        if (vinculo is null || vinculo.Situacao != StatusVinculo.Ativo)
            throw AcessoNegado();

        return (perfil, vinculo);
    }

    private static DisponibilidadeSemanalResponse ParaResponse(DisponibilidadeSemanal janela) =>
        new(janela.Id, janela.ProfissionalId, (int)janela.DiaSemana, janela.IniciaEm, janela.TerminaEm);

    private static ExcecaoAutorizacaoAplicacao AcessoNegado() =>
        new(CodigosErro.TenantNegado, "O usuário não possui vínculo ativo com a barbearia.");

    private static ExcecaoAutorizacaoAplicacao AcessoNegado(string mensagem) =>
        new(CodigosErro.TenantNegado, mensagem);
}
