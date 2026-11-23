import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';

import { BarbeariaService } from '../barbearia/barbearia.service';
import { EmptyState } from '../shared/ui/empty-state/empty-state';
import { Table } from '../shared/ui/table/table';
import { DashboardService } from './dashboard.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EmptyState, Table],
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit {
  private readonly servico = inject(DashboardService);
  private readonly barbearias = inject(BarbeariaService);

  readonly resumo = this.servico.resumo;
  readonly carregando = this.servico.carregando;
  readonly erro = this.servico.erro;
  readonly fuso = computed(() => this.barbearias.barbearia()?.fusoHorarioId);

  ngOnInit(): void {
    const barbeariaId = this.barbearias.barbeariaId();
    if (barbeariaId) this.servico.carregar(barbeariaId);
  }

  formatarData(instant: string): string {
    return new Intl.DateTimeFormat('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: this.fuso() || undefined,
    }).format(new Date(instant));
  }

  formatarValor(valor: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor);
  }

  rotuloSituacao(situacao: string): string {
    const rotulos: Record<string, string> = {
      Confirmado: 'Confirmado',
      EmAndamento: 'Em andamento',
      Concluido: 'Concluído',
      Cancelado: 'Cancelado',
      NaoCompareceu: 'Não compareceu',
    };
    return rotulos[situacao] ?? situacao;
  }
}
