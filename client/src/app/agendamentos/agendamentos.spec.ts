import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../auth/auth.service';
import { BarbeariaService } from '../barbearia/barbearia.service';
import { violacoesCriticas } from '../../testing/axe';
import { Agenda } from './agenda/agenda';
import { AgendamentoPublico } from './publico/publico';
import { AgendamentosService } from './agendamentos.service';

describe('AgendamentosService', () => {
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

  it('should list the agenda with period and professional filters', () => {
    servico.listarAgenda('b1', '2026-11-22', '2026-11-29', 'p1');
    const req = http.expectOne(
      '/api/v1/barbearias/b1/agendamentos?de=2026-11-22&ate=2026-11-29&profissionalId=p1',
    );
    expect(req.request.method).toBe('GET');
    req.flush([]);
    expect(servico.agenda()).toEqual([]);
  });

  it('should confirm a public appointment', () => {
    servico.confirmar('b1', {
      profissionalId: 'p1',
      servicoId: 's1',
      iniciaEmUtc: '2026-11-23T13:00:00Z',
    }, true);

    const req = http.expectOne('/api/v1/barbearias/b1/agendamentos/public');
    expect(req.request.method).toBe('POST');
    expect(servico.carregando()).toBe(true);
    req.flush({});
    expect(servico.sucesso()).toContain('confirmado');
  });

  it('should query available slots', () => {
    servico.consultarSlots('b1', 's1', 'p1', '2026-11-23');
    const req = http.expectOne(
      '/api/v1/barbearias/b1/disponibilidade?servicoId=s1&profissionalId=p1&date=2026-11-23&dias=1',
    );
    req.flush({ date: '2026-11-23', fusoHorarioId: 'America/Sao_Paulo', slots: [], hasAvailability: false });
    expect(servico.slots()).toEqual([]);
  });
});

describe('Telas de agendamento', () => {
  async function configurar() {
    await TestBed.configureTestingModule({
      imports: [Agenda, AgendamentoPublico],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: AuthService,
          useValue: { conta: signal({ tipo: 'Professional' }), erro: signal(null) },
        },
        {
          provide: BarbeariaService,
          useValue: {
            barbeariaId: signal('b1'),
            barbearia: signal({ codigoConvite: 'ABC23456', fusoHorarioId: 'America/Sao_Paulo' }),
            vinculo: signal({ profissionalId: 'p1', cargo: 'Proprietario' }),
            profissionais: signal([
              { id: 'p1', nomeCompleto: 'Dono', telefone: null, situacao: 'Ativo' },
              { id: 'p2', nomeCompleto: 'Barbeiro', telefone: null, situacao: 'Ativo' },
            ]),
            obter: vi.fn(),
            listarProfissionais: vi.fn(),
          },
        },
        {
          provide: AgendamentosService,
          useValue: {
            agenda: signal([{
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
            }]),
            bloqueios: signal([]),
            servicos: signal([]),
            slots: signal([]),
            carregando: signal(false),
            erro: signal(null),
            sucesso: signal(null),
            listarAgenda: vi.fn(),
            listarBloqueios: vi.fn(),
            listarServicos: vi.fn(),
            consultarSlots: vi.fn(),
            confirmar: vi.fn(),
            limparMensagens: vi.fn(),
          },
        },
      ],
    }).compileComponents();
  }

  it('should render agenda with day, week and month views', async () => {
    await configurar();
    const fixture = TestBed.createComponent(Agenda);
    await fixture.whenStable();
    const elemento = fixture.nativeElement as HTMLElement;

    expect(elemento.textContent).toContain('Corte degradê');
    expect(elemento.textContent).toContain('Dia');
    expect(elemento.textContent).toContain('Semana');
    expect(elemento.textContent).toContain('Mês');
    expect(elemento.querySelector('select#profissional-agenda')).not.toBeNull();
  });

  it('should not have critical axe violations on agenda', async () => {
    await configurar();
    const fixture = TestBed.createComponent(Agenda);
    await fixture.whenStable();
    expect(await violacoesCriticas(fixture.nativeElement as HTMLElement)).toEqual([]);
  });

  it('should not confirm a public appointment without a selected slot', async () => {
    await configurar();
    const fixture = TestBed.createComponent(AgendamentoPublico);
    await fixture.whenStable();
    const elemento = fixture.nativeElement as HTMLElement;
    const botao = elemento.querySelector<HTMLButtonElement>('app-button button')!;

    expect(botao.disabled).toBe(true);
  });
});
