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
import { MeusAgendamentos } from './meus/meus';

const agendamento = {
  id: 'a1',
  barbeariaId: 'b1',
  profissionalId: 'p1',
  servicoId: 's1',
  iniciaEmUtc: '2026-11-23T13:00:00Z',
  terminaEmUtc: '2026-11-23T13:40:00Z',
  nomeServico: 'Corte degradê',
  duracaoMinutos: 40,
  preco: 80,
  situacao: 'Confirmado',
  origem: 'Publico',
};

const sugestao = {
  id: 'sg1',
  agendamentoId: 'a1',
  profissionalId: 'p1',
  inicioSugeridoUtc: '2026-12-01T13:00:00Z',
  fimSugeridoUtc: '2026-12-01T13:40:00Z',
  motivo: 'Horário melhor',
  situacao: 'Pendente' as const,
  expiraEmUtc: new Date(Date.now() + 3600_000).toISOString(),
  respondidoEmUtc: null,
  autorUsuarioId: 'p1',
};

const historico = [
  {
    id: 'h1',
    agendamentoId: 'a1',
    tipoEvento: 'Criacao',
    atorUsuarioId: null,
    ocorridoEmUtc: '2026-11-20T10:00:00Z',
    inicioAnteriorUtc: null,
    novoInicioUtc: null,
    detalhes: null,
  },
  {
    id: 'h2',
    agendamentoId: 'a1',
    tipoEvento: 'Cancelamento',
    atorUsuarioId: 'cliente-1',
    ocorridoEmUtc: '2026-11-21T10:00:00Z',
    inicioAnteriorUtc: null,
    novoInicioUtc: null,
    detalhes: 'Desistência',
  },
];

describe('Serviço do ciclo de vida', () => {
  let servico: AgendamentosService;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
    servico = TestBed.inject(AgendamentosService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should cancel an appointment', () => {
    const concluido = vi.fn();
    servico.cancelar('b1', 'a1', 'Desistência', concluido);
    const req = http.expectOne('/api/v1/barbearias/b1/agendamentos/a1/cancelar');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ motivo: 'Desistência' });
    req.flush({});
    expect(concluido).toHaveBeenCalledOnce();
  });

  it('should reschedule and suggest with server-side time', () => {
    servico.remarcar('b1', 'a1', '2026-11-24T13:00:00Z', vi.fn());
    http.expectOne('/api/v1/barbearias/b1/agendamentos/a1/remarcar').flush({});

    servico.sugerir('b1', 'a1', '2026-11-25T13:00:00Z', 'Troca', vi.fn());
    const sugestaoReq = http.expectOne('/api/v1/barbearias/b1/agendamentos/a1/sugestoes');
    expect(sugestaoReq.request.body).toEqual({
      iniciaEmUtc: '2026-11-25T13:00:00Z',
      motivo: 'Troca',
    });
    sugestaoReq.flush({});
  });

  it('should accept a suggestion', () => {
    servico.responderSugestao('b1', 'a1', 'sg1', true, vi.fn());
    const req = http.expectOne('/api/v1/barbearias/b1/agendamentos/a1/sugestoes/sg1/aceitar');
    expect(req.request.method).toBe('POST');
    req.flush({});
    expect(servico.sucesso()).toContain('aceita');
  });
});

describe('Telas do ciclo de vida', () => {
  async function configurar(tipo: string) {
    await TestBed.configureTestingModule({
      imports: [DetalheAgendamento, MeusAgendamentos],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: AuthService,
          useValue: { conta: signal({ tipo }), erro: signal(null), limparErro: vi.fn() },
        },
        {
          provide: BarbeariaService,
          useValue: {
            barbeariaId: signal('b1'),
            barbearia: signal({ codigoConvite: 'ABC23456', fusoHorarioId: 'America/Sao_Paulo' }),
            vinculo: signal({ profissionalId: 'p1' }),
            profissionais: signal([]),
            dadosPublicos: signal(null),
            obter: vi.fn(),
            obterDadosAgendamentoPublico: vi.fn(),
            listarProfissionais: vi.fn(),
            selecionar: vi.fn(),
          },
        },
        {
          provide: AgendamentosService,
          useValue: {
            detalhe: signal(agendamento),
            detalheId: signal('a1'),
            historico: signal(historico),
            sugestoes: signal([sugestao]),
            slots: signal([]),
            agenda: signal([agendamento]),
            carregando: signal(false),
            erro: signal(null),
            sucesso: signal(null),
            limparMensagens: vi.fn(),
            carregarDetalhe: vi.fn(),
            carregarHistorico: vi.fn(),
            carregarSugestoes: vi.fn(),
            consultarSlots: vi.fn(),
            cancelar: vi.fn(),
            remarcar: vi.fn(),
            sugerir: vi.fn(),
            responderSugestao: vi.fn(),
            listarMeus: vi.fn(),
            selecionarDetalhe: vi.fn(),
          },
        },
      ],
    }).compileComponents();
  }

  it('should render details, suggestion and chronological history for a customer', async () => {
    await configurar('Customer');
    const fixture = TestBed.createComponent(DetalheAgendamento);
    await fixture.whenStable();
    const elemento = fixture.nativeElement as HTMLElement;

    expect(elemento.textContent).toContain('Corte degradê');
    expect(elemento.textContent).toContain('Horário melhor');
    expect(elemento.textContent).toContain('Válido até');
    expect(elemento.textContent).toContain('Criação');
    expect(elemento.textContent).toContain('Cancelamento');
    expect(elemento.querySelector('select#detalhe-horario')).not.toBeNull();
  });

  it('should not offer suggestion to a customer', async () => {
    await configurar('Customer');
    const fixture = TestBed.createComponent(DetalheAgendamento);
    await fixture.whenStable();
    const elemento = fixture.nativeElement as HTMLElement;

    expect(elemento.textContent).not.toContain('Sugerir novo horário');
  });

  it('should offer suggestion to a professional', async () => {
    await configurar('Professional');
    const fixture = TestBed.createComponent(DetalheAgendamento);
    await fixture.whenStable();
    const elemento = fixture.nativeElement as HTMLElement;

    expect(elemento.textContent).toContain('Sugerir novo horário');
  });

  it('should ask for confirmation before cancelling', async () => {
    await configurar('Customer');
    const fixture = TestBed.createComponent(DetalheAgendamento);
    await fixture.whenStable();
    const elemento = fixture.nativeElement as HTMLElement;
    const botoes = [...elemento.querySelectorAll<HTMLButtonElement>('app-button button')];
    const cancelar = botoes.find((botao) => botao.textContent?.trim() === 'Cancelar agendamento')!;

    cancelar.click();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Confirmar cancelamento');
  });

  it('should list own appointments and request detail', async () => {
    await configurar('Customer');
    const fixture = TestBed.createComponent(MeusAgendamentos);
    await fixture.whenStable();
    const elemento = fixture.nativeElement as HTMLElement;

    expect(elemento.textContent).toContain('Meus agendamentos');
    expect(elemento.textContent).toContain('Corte degradê');
    expect(elemento.querySelector('app-button button')).not.toBeNull();
  });

  it('should not have critical axe violations', async () => {
    await configurar('Customer');
    const detalhe = TestBed.createComponent(DetalheAgendamento);
    await detalhe.whenStable();
    expect(await violacoesCriticas(detalhe.nativeElement as HTMLElement)).toEqual([]);

    const meus = TestBed.createComponent(MeusAgendamentos);
    await meus.whenStable();
    expect(await violacoesCriticas(meus.nativeElement as HTMLElement)).toEqual([]);
  });
});
