import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { violacoesCriticas } from '../../testing/axe';
import { Notificacoes } from './notificacoes';
import { NotificacoesService } from './notificacoes.service';

const notificacao = {
  id: 'n1',
  barbeariaId: 'b1',
  tipo: 'AgendamentoCriado',
  agendamentoId: 'a1',
  mensagem: 'Novo agendamento confirmado.',
  criadoEmUtc: '2026-11-23T10:00:00Z',
  lidoEmUtc: null,
};

describe('Serviço de notificações', () => {
  let servico: NotificacoesService;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
    servico = TestBed.inject(NotificacoesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    servico.pararPolling();
    http.verify();
  });

  it('should load notifications and unread count', () => {
    servico.carregar();
    const req = http.expectOne('/api/v1/notificacoes');
    req.flush({ itens: [notificacao], naoLidas: 1, pagina: 1, tamanho: 20 });
    expect(servico.itens()).toEqual([notificacao]);
    expect(servico.naoLidas()).toBe(1);
  });

  it('should mark one and all as read', () => {
    servico.marcarComoLida('n1');
    const um = http.expectOne('/api/v1/notificacoes/n1/ler');
    expect(um.request.method).toBe('POST');
    um.flush(null);
    http
      .expectOne('/api/v1/notificacoes')
      .flush({ itens: [], naoLidas: 0, pagina: 1, tamanho: 20 });

    servico.marcarTodas();
    const todas = http.expectOne('/api/v1/notificacoes/ler-todas');
    expect(todas.request.method).toBe('POST');
    todas.flush({ marcadas: 0 });
    http
      .expectOne('/api/v1/notificacoes')
      .flush({ itens: [], naoLidas: 0, pagina: 1, tamanho: 20 });
  });
});

describe('Central de notificações', () => {
  async function configurar(itens: (typeof notificacao)[] = [notificacao], naoLidas = 1) {
    await TestBed.configureTestingModule({
      imports: [Notificacoes],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: NotificacoesService,
          useValue: {
            itens: signal(itens),
            naoLidas: signal(naoLidas),
            carregando: signal(false),
            erro: signal(null),
            atualizando: signal(false),
            carregar: vi.fn(),
            marcarComoLida: vi.fn(),
            marcarTodas: vi.fn(),
          },
        },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(Notificacoes);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should render notifications and unread badge', async () => {
    const elemento = await configurar();
    expect(elemento.textContent).toContain('Novo agendamento confirmado.');
    expect(elemento.textContent).toContain('1 não lidas');
    expect(elemento.textContent).toContain('Marcar todas como lidas');
  });

  it('should render empty state', async () => {
    const elemento = await configurar([], 0);
    expect(elemento.textContent).toContain('Nenhuma notificação');
  });

  it('should not have critical axe violations', async () => {
    expect(await violacoesCriticas(await configurar())).toEqual([]);
  });
});
