import { HttpClient } from '@angular/common/http';
import { inject, Injectable, OnDestroy, signal } from '@angular/core';
import { EMPTY, exhaustMap, finalize, interval, Subject, takeUntil } from 'rxjs';

import { ErroApi } from '../core/http/erro-api';
import { type ListaNotificacoes, type Notificacao } from './notificacoes.models';

/** Polling a cada 60 s (ADR-016), sem requisições sobrepostas. */
const INTERVALO_POLLING_MS = 60_000;

@Injectable({ providedIn: 'root' })
export class NotificacoesService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly parar$ = new Subject<void>();
  private pollingAtivo = false;

  private readonly itensState = signal<Notificacao[]>([]);
  private readonly naoLidasState = signal(0);
  private readonly carregandoState = signal(false);
  private readonly erroState = signal<string | null>(null);
  private readonly atualizandoState = signal(false);

  readonly itens = this.itensState.asReadonly();
  readonly naoLidas = this.naoLidasState.asReadonly();
  readonly carregando = this.carregandoState.asReadonly();
  readonly erro = this.erroState.asReadonly();
  readonly atualizando = this.atualizandoState.asReadonly();

  carregar(): void {
    this.carregandoState.set(true);
    this.executarListagem(true);
  }

  iniciarPolling(): void {
    if (this.pollingAtivo) return;
    this.pollingAtivo = true;

    interval(INTERVALO_POLLING_MS)
      .pipe(
        takeUntil(this.parar$),
        // Documento oculto não dispara requisição; exhaustMap evita sobreposição.
        exhaustMap(() =>
          typeof document !== 'undefined' && document.visibilityState !== 'visible'
            ? EMPTY
            : this.http.get<ListaNotificacoes>('/api/v1/notificacoes', { withCredentials: true }),
        ),
      )
      .subscribe({
        next: (resposta) => this.aplicar(resposta),
        error: (erro: unknown) =>
          this.erroState.set(
            erro instanceof ErroApi ? erro.message : 'Não foi possível atualizar as notificações.',
          ),
      });
  }

  pararPolling(): void {
    if (!this.pollingAtivo) return;
    this.parar$.next();
    this.pollingAtivo = false;
  }

  marcarComoLida(id: string): void {
    this.atualizandoState.set(true);
    this.http
      .post(`/api/v1/notificacoes/${id}/ler`, {}, { withCredentials: true })
      .pipe(finalize(() => this.atualizandoState.set(false)))
      .subscribe({
        next: () => this.carregar(),
        error: (erro: unknown) =>
          this.erroState.set(
            erro instanceof ErroApi ? erro.message : 'Não foi possível marcar como lida.',
          ),
      });
  }

  marcarTodas(): void {
    this.atualizandoState.set(true);
    this.http
      .post('/api/v1/notificacoes/ler-todas', {}, { withCredentials: true })
      .pipe(finalize(() => this.atualizandoState.set(false)))
      .subscribe({
        next: () => this.carregar(),
        error: (erro: unknown) =>
          this.erroState.set(
            erro instanceof ErroApi ? erro.message : 'Não foi possível marcar todas.',
          ),
      });
  }

  ngOnDestroy(): void {
    this.pararPolling();
    this.parar$.complete();
  }

  private executarListagem(marcarCarregando: boolean): void {
    this.http
      .get<ListaNotificacoes>('/api/v1/notificacoes', { withCredentials: true })
      .pipe(finalize(() => marcarCarregando && this.carregandoState.set(false)))
      .subscribe({
        next: (resposta) => this.aplicar(resposta),
        error: (erro: unknown) => {
          if (marcarCarregando) this.carregandoState.set(false);
          this.erroState.set(
            erro instanceof ErroApi ? erro.message : 'Não foi possível carregar as notificações.',
          );
        },
      });
  }

  private aplicar(resposta: ListaNotificacoes): void {
    this.itensState.set(resposta.itens);
    this.naoLidasState.set(resposta.naoLidas);
    this.erroState.set(null);
  }
}
