import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { BarbeariaService } from '../barbearia/barbearia.service';
import { violacoesCriticas } from '../../testing/axe';
import { Dashboard } from './dashboard';
import { DashboardService } from './dashboard.service';

const resumo = {
  hoje: 3,
  estaSemana: 12,
  cancelamentos: 2,
  recebidoNoMes: 1250.5,
  proximos: [
    {
      id: 'a1',
      iniciaEmUtc: '2026-11-23T13:00:00Z',
      nomeCliente: 'Cliente Um',
      nomeProfissional: 'Barbeiro Um',
      situacao: 'Confirmado',
    },
  ],
};

describe('Serviço do dashboard', () => {
  let servico: DashboardService;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    servico = TestBed.inject(DashboardService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should load the owner dashboard from the tenant route', () => {
    servico.carregar('b1');
    const req = http.expectOne('/api/v1/barbearias/b1/dashboard');
    expect(req.request.method).toBe('GET');
    req.flush(resumo);
    expect(servico.resumo()?.hoje).toBe(3);
  });

  it('should expose an error message on failure', () => {
    servico.carregar('b1');
    http.expectOne('/api/v1/barbearias/b1/dashboard').flush('erro', { status: 500, statusText: 'Erro' });
    expect(servico.erro()).toContain('Não foi possível carregar');
  });
});

describe('Tela do dashboard', () => {
  async function configurar(dados: typeof resumo | null = resumo) {
    await TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: BarbeariaService,
          useValue: {
            barbeariaId: signal('b1'),
            barbearia: signal({ fusoHorarioId: 'America/Sao_Paulo' }),
          },
        },
        {
          provide: DashboardService,
          useValue: {
            resumo: signal(dados),
            carregando: signal(false),
            erro: signal(null),
            carregar: vi.fn(),
          },
        },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(Dashboard);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should render indicators and upcoming appointments', async () => {
    const elemento = await configurar();
    expect(elemento.textContent).toContain('Hoje');
    expect(elemento.textContent).toContain('Esta semana');
    expect(elemento.textContent).toContain('Cancelamentos');
    expect(elemento.textContent).toContain('Recebido no mês');
    expect(elemento.textContent).toContain('Cliente Um');
    expect(elemento.textContent).toContain('Barbeiro Um');
    expect(elemento.textContent).toContain('Confirmado');
  });

  it('should render zeros and empty state without data movements', async () => {
    const elemento = await configurar({
      hoje: 0,
      estaSemana: 0,
      cancelamentos: 0,
      recebidoNoMes: 0,
      proximos: [],
    });
    expect(elemento.textContent).toContain('Nenhum agendamento futuro');
    expect(elemento.textContent).toContain('0,00');
  });

  it('should not have critical axe violations', async () => {
    expect(await violacoesCriticas(await configurar())).toEqual([]);
  });
});
