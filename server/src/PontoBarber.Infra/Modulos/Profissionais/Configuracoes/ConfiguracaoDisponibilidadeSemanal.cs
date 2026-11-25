using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Profissionais.Configuracoes;

/// <summary>Mapeamento da disponibilidade semanal (TDD §8.3).</summary>
public sealed class ConfiguracaoDisponibilidadeSemanal : ConfiguracaoEntidade<DisponibilidadeSemanal>
{
    public override void Configure(EntityTypeBuilder<DisponibilidadeSemanal> builder)
    {
        base.Configure(builder);

        builder.ToTable("DisponibilidadesSemanais");
        builder.Property(janela => janela.ProfissionalId).IsRequired();
        builder.Property(janela => janela.DiaSemana).HasConversion<int>().IsRequired();

        builder.HasIndex(janela => new { janela.BarbeariaId, janela.ProfissionalId, janela.DiaSemana })
            .IsUnique()
            .HasDatabaseName("UX_Disponibilidade_Barbearia_Profissional_Dia");

        builder.HasOne<BarbeariaEntidade>()
            .WithMany()
            .HasForeignKey(janela => janela.BarbeariaId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<VinculoProfissional>()
            .WithMany()
            .HasForeignKey(janela => new { janela.BarbeariaId, janela.ProfissionalId })
            .HasPrincipalKey(vinculo => new { vinculo.BarbeariaId, vinculo.ProfissionalId })
            .OnDelete(DeleteBehavior.NoAction);
    }
}
