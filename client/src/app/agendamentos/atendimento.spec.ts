import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../auth/auth.service';
import { BarbeariaService } from '../barbearia/barbearia.service';
import { violacoesCriticas } from '../../testing/axe';
import { AgendamentosService } from './agendamentos.service';
import { DetalheAgendamento } from './detalhe/detalhe';

const agendamentoBase = {
  id: 'a1',
  barbeariaId: 'b1',
  profissionalId: 'p1',
  servicoId: 's1',
  iniciaEmUtc: '2026-11-22T13:00:00Z',
  terminaEmUtc: '2026-11-22T13:40:00Z',
  nomeServico: 'Corte degradê',
  duracaoMinutos: 40,
  preco: 80,
  situacao: 'Confirmado',
  origem: 'Publico',
  inicioRealUtc: null,
  fimRealUtc: null,
  situacaoPagamento: 'Pendente',
  pagamento: null,
};

describe('Serviço de atendimento', () => {
  let servico: AgendamentosService;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    servico = TestBed.inject(AgendamentosService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should start and finish attendance', () => {
    servico.iniciar('b1', 'a1', vi.fn());
    http.expectOne('/api/v1/barbearias/b1/agendamentos/a1/iniciar').flush({});

    servico.concluir('b1', 'a1', vi.fn());
    const req = http.expectOne('/api/v1/barbearias/b1/agendamentos/a1/concluir');
    expect(req.request.method).toBe('POST');
    req.flush({});
    expect(servico.sucesso()).toContain('concluído');
  });

  it('should register absence', () => {
    servico.registrarAusencia('b1', 'a1', vi.fn());
    http.expectOne('/api/v1/barbearias/b1/agendamentos/a1/ausencia').flush({});
    expect(servico.sucesso()).toContain('comparecimento');
  });

  it('should send idempotency key when registering payment', () => {
    servico.registrarPagamento('b1', 'a1', 80, 'Pix', vi.fn());
    const req = http.expectOne('/api/v1/barbearias/b1/agendamentos/a1/pagamento');
    expect(req.request.body).toEqual({ valor: 80, metodo: 'Pix' });
    expect(req.request.headers.has('Idempotency-Chave')).toBe(true);
    req.flush({});
    expect(servico.sucesso()).toContain('Pagamento registrado');
  });
});

describe('Tela de atendimento', () => {
  async function configurar(detalhe: Record<string, unknown>, cargo: string | null) {
    await TestBed.configureTestingModule({
      imports: [DetalheAgendamento],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: AuthService,
          useValue: { conta: signal({ tipo: cargo ? 'Professional' : 'Customer' }), erro: signal(null) },
        },
        {
          provide: BarbeariaService,
          useValue: {
            barbeariaId: signal('b1'),
            barbearia: signal({ fusoHorarioId: 'America/Sao_Paulo' }),
            vinculo: signal(cargo ? { cargo, profissionalId: 'p1' } : null),
            obter: vi.fn(),
            listarProfissionais: vi.fn(),
          },
        },
        {
          provide: AgendamentosService,
          useValue: {
            detalhe: signal(detalhe),
            detalheId: signal('a1'),
            historico: signal([]),
            sugestoes: signal([]),
            slots: signal([]),
            carregando: signal(false),
            erro: signal(null),
            sucesso: signal(null),
            limparMensagens: vi.fn(),
            carregarDetalhe: vi.fn(),
            consultarSlots: vi.fn(),
            cancelar: vi.fn(),
            remarcar: vi.fn(),
            sugerir: vi.fn(),
            responderSugestao: vi.fn(),
            iniciar: vi.fn(),
            concluir: vi.fn(),
            registrarAusencia: vi.fn(),
            registrarPagamento: vi.fn(),
          },
        },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(DetalheAgendamento);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should offer attendance actions only to the barber', async () => {
    const elemento = await configurar(agendamentoBase, 'Barbeiro');
    expect(elemento.textContent).toContain('Iniciar atendimento');
    expect(elemento.textContent).toContain('Marcar não comparecimento');
    expect(elemento.textContent).toContain('Registrar pagamento');
  });

  it('should hide attendance actions from the customer', async () => {
    const elemento = await configurar(agendamentoBase, null);
    expect(elemento.textContent).not.toContain('Iniciar atendimento');
    expect(elemento.textContent).not.toContain('Registrar pagamento');
  });

  it('should show real times and pending payment after completion', async () => {
    const elemento = await configurar({
      ...agendamentoBase,
      situacao: 'Concluido',
      inicioRealUtc: '2026-11-22T13:02:00Z',
      fimRealUtc: '2026-11-22T13:45:00Z',
    }, 'Barbeiro');

    expect(elemento.textContent).toContain('Início real');
    expect(elemento.textContent).toContain('Fim real');
    expect(elemento.textContent).toContain('Atendimento finalizado sem pagamento registrado.');
    expect(elemento.textContent).not.toContain('Concluir atendimento');
  });

  it('should distinguish no-show from cancellation', async () => {
    const elemento = await configurar({ ...agendamentoBase, situacao: 'NaoCompareceu' }, 'Barbeiro');
    expect(elemento.textContent).toContain('NaoCompareceu');
    expect(elemento.textContent).toContain('Este estado é distinto de cancelamento.');
    expect(elemento.textContent).not.toContain('Iniciar atendimento');
    expect(elemento.textContent).not.toContain('Registrar pagamento');
  });

  it('should not have critical axe violations', async () => {
    const elemento = await configurar(agendamentoBase, 'Barbeiro');
    expect(await violacoesCriticas(elemento)).toEqual([]);
  });
});
