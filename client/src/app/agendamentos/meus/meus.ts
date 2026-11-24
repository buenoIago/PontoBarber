import { ChangeDetectionStrategy, Component, inject, OnInit, output } from '@angular/core';

import { BarbeariaService } from '../../barbearia/barbearia.service';
import { Button } from '../../shared/ui/button/button';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { Table } from '../../shared/ui/table/table';
import { type Agendamento } from '../agendamentos.models';
import { AgendamentosService } from '../agendamentos.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState, Table],
  selector: 'app-meus-agendamentos',
  templateUrl: './meus.html',
})
export class MeusAgendamentos implements OnInit {
  private readonly servico = inject(AgendamentosService);
  private readonly barbearias = inject(BarbeariaService);

  readonly detalheSolicitado = output<void>();
  readonly agenda = this.servico.agenda;
  readonly carregando = this.servico.carregando;
  readonly erro = this.servico.erro;

  ngOnInit(): void {
    this.servico.limparMensagens();
    const hoje = new Date();
    const fim = new Date();
    fim.setDate(fim.getDate() + 30);
    this.servico.listarMeus(this.paraData(hoje), this.paraData(fim));
  }

  abrir(agendamento: Agendamento): void {
    this.barbearias.selecionar(agendamento.barbeariaId);
    this.barbearias.obterDadosAgendamentoPublico(agendamento.barbeariaId);
    this.servico.selecionarDetalhe(agendamento.id);
    this.detalheSolicitado.emit();
  }

  formatar(instant: string): string {
    return new Intl.DateTimeFormat('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: this.barbearias.dadosPublicos()?.fusoHorarioId || undefined,
    }).format(new Date(instant));
  }

  private paraData(data: Date): string {
    return `${data.getFullYear()}-${String(data.getMonth() + 1).padStart(2, '0')}-${String(data.getDate()).padStart(2, '0')}`;
  }
}
