import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';

import { Button } from '../shared/ui/button/button';
import { EmptyState } from '../shared/ui/empty-state/empty-state';
import { NotificacoesService } from './notificacoes.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, EmptyState],
  selector: 'app-notificacoes',
  templateUrl: './notificacoes.html',
})
export class Notificacoes implements OnInit {
  private readonly servico = inject(NotificacoesService);

  readonly itens = this.servico.itens;
  readonly naoLidas = this.servico.naoLidas;
  readonly carregando = this.servico.carregando;
  readonly erro = this.servico.erro;
  readonly atualizando = this.servico.atualizando;

  ngOnInit(): void {
    this.servico.carregar();
  }

  marcarComoLida(id: string): void {
    this.servico.marcarComoLida(id);
  }

  marcarTodas(): void {
    this.servico.marcarTodas();
  }

  formatar(instant: string): string {
    return new Intl.DateTimeFormat('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(instant));
  }

  rotulo(tipo: string): string {
    const rotulos: Record<string, string> = {
      AgendamentoCriado: 'Agendamento criado',
      AgendamentoCancelado: 'Agendamento cancelado',
      SugestaoCriada: 'Sugestão de horário',
      SugestaoAceita: 'Sugestão aceita',
      SugestaoRejeitada: 'Sugestão rejeitada',
      SugestaoExpirada: 'Sugestão expirada',
      ServicoIniciado: 'Atendimento iniciado',
      ServicoConcluido: 'Atendimento concluído',
      PagamentoRegistrado: 'Pagamento registrado',
    };
    return rotulos[tipo] ?? tipo;
  }
}
