using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;

namespace PontoBarber.Aplicacao.Modulos.Barbearia;

/// <summary>
/// Configuração semanal de funcionamento (Task 6.1). Somente o proprietário
/// altera; a leitura exige vínculo ativo na barbearia da rota.
/// </summary>
public sealed class GestaoHorariosFuncionamento(
    IHorarioFuncionamentoRepositorio repositorio,
    IProfissionalRepositorio profissionais)
{
    public async Task<IReadOnlyList<DiaFuncionamentoResponse>> ListarAsync(
        Guid barbeariaId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ObterVinculoAtivoAsync(barbeariaId, usuarioId, cancellationToken);

        var dias = await repositorio.ListarPorBarbeariaAsync(barbeariaId, cancellationToken);
        return dias.OrderBy(dia => (int)dia.DiaSemana).Select(ParaResponse).ToList();
    }

    public async Task<IReadOnlyList<DiaFuncionamentoResponse>> SalvarAsync(
        Guid barbeariaId,
        IReadOnlyList<DiaFuncionamentoRequest> request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var vinculo = await ObterVinculoAtivoAsync(barbeariaId, usuarioId, cancellationToken);
        if (vinculo.Cargo != CargoProfissional.Proprietario)
            throw AcessoNegado("Somente o proprietário pode configurar os horários gerais.");

        ValidarSemana(request, barbeariaId);

        var existentes = await repositorio.ListarPorBarbeariaAsync(barbeariaId, cancellationToken);
        var novos = new List<HorarioFuncionamento>();

        foreach (var dia in request)
        {
            var diaSemana = (DiaSemana)dia.DiaSemana;
            var existente = existentes.FirstOrDefault(item => item.DiaSemana == diaSemana);
            if (existente is not null)
                existente.AtualizarDados(dia.AbreAs, dia.FechaAs, dia.Fechado);
            else
                novos.Add(HorarioFuncionamento.Criar(
                    barbeariaId, diaSemana, dia.AbreAs, dia.FechaAs, dia.Fechado));
        }

        if (novos.Count > 0)
            await repositorio.AdicionarAsync(novos, cancellationToken);
        await repositorio.SalvarAlteracoesAsync(cancellationToken);

        var salvos = await repositorio.ListarPorBarbeariaAsync(barbeariaId, cancellationToken);
        return salvos.OrderBy(dia => (int)dia.DiaSemana).Select(ParaResponse).ToList();
    }

    private static void ValidarSemana(IReadOnlyList<DiaFuncionamentoRequest> request, Guid barbeariaId)
    {
        if (request.Count != 7 ||
            request.Select(dia => dia.DiaSemana).Distinct().Count() != 7 ||
            request.Any(dia => !Enum.IsDefined(typeof(DiaSemana), dia.DiaSemana)))
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["dias"] = ["Informe exatamente um registro para cada dia da semana."]
            });

        foreach (var dia in request)
        {
            var erros = HorarioFuncionamento.ValidarDados(
                barbeariaId, (DiaSemana)dia.DiaSemana, dia.AbreAs, dia.FechaAs, dia.Fechado);
            if (erros.Count > 0)
                throw new ExcecaoValidacaoAplicacao(
                    erros.ToDictionary(erro => erro.Campo, erro => new[] { erro.Mensagem }));
        }
    }

    private async Task<VinculoProfissional> ObterVinculoAtivoAsync(
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

        return vinculo;
    }

    private static DiaFuncionamentoResponse ParaResponse(HorarioFuncionamento horario) =>
        new((int)horario.DiaSemana, horario.AbreAs, horario.FechaAs, horario.Fechado);

    private static ExcecaoAutorizacaoAplicacao AcessoNegado() =>
        new(CodigosErro.TenantNegado, "O usuário não possui vínculo ativo com a barbearia.");

    private static ExcecaoAutorizacaoAplicacao AcessoNegado(string mensagem) =>
        new(CodigosErro.TenantNegado, mensagem);
}
