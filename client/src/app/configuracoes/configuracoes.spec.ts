import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection, signal, type Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../auth/auth.service';
import { BarbeariaService } from '../barbearia/barbearia.service';
import { violacoesCriticas } from '../../testing/axe';
import { Bloqueios } from './bloqueios/bloqueios';
import { Disponibilidade } from './disponibilidade/disponibilidade';
import { Horarios } from './horarios/horarios';
import { ConfiguracoesService } from './configuracoes.service';

const semana = [
  { diaSemana: 1, abreAs: '09:00:00', fechaAs: '18:00:00', fechado: false },
  { diaSemana: 7, abreAs: null, fechaAs: null, fechado: true },
];

describe('ConfiguracoesService', () => {
  let servico: ConfiguracoesService;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();
    servico = TestBed.inject(ConfiguracoesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should read and save weekly opening hours', () => {
    servico.listarHorarios('b1');
    http.expectOne('/api/v1/barbearias/b1/horarios-funcionamento').flush(semana);
    expect(servico.horarios()).toEqual(semana);

    servico.salvarHorarios('b1', semana);
    const req = http.expectOne('/api/v1/barbearias/b1/horarios-funcionamento');
    expect(req.request.method).toBe('PUT');
    req.flush(semana);
    expect(servico.sucesso()).toContain('Horários');
  });

  it('should read and save weekly availability', () => {
    const janela = { id: 'j1', diaSemana: 1, iniciaEm: '10:00:00', terminaEm: '12:00:00' };
    servico.listarDisponibilidade('b1', 'p1');
    http.expectOne('/api/v1/barbearias/b1/profissionais/p1/disponibilidade').flush([janela]);

    servico.salvarDisponibilidade('b1', 'p1', [janela]);
    const req = http.expectOne('/api/v1/barbearias/b1/profissionais/p1/disponibilidade');
    expect(req.request.method).toBe('PUT');
    req.flush([janela]);
    expect(servico.disponibilidade()).toEqual([janela]);
  });

  it('should create and remove blocks', () => {
    servico.criarBloqueio('b1', 'p1', {
      iniciaEmUtc: '2026-11-30T13:00:00Z',
      terminaEmUtc: '2026-11-30T14:00:00Z',
      motivo: 'Almoço',
    });
    const criacao = http.expectOne('/api/v1/barbearias/b1/profissionais/p1/blocks');
    expect(criacao.request.method).toBe('POST');
    criacao.flush(null);
    http.expectOne('/api/v1/barbearias/b1/profissionais/p1/blocks').flush([]);

    servico.removerBloqueio('b1', 'p1', 'bl1');
    const remocao = http.expectOne('/api/v1/barbearias/b1/profissionais/p1/blocks/bl1');
    expect(remocao.request.method).toBe('DELETE');
    remocao.flush(null);
    http.expectOne('/api/v1/barbearias/b1/profissionais/p1/blocks').flush([]);
  });
});

describe('Telas de configuração', () => {
  async function configurar() {
    await TestBed.configureTestingModule({
      imports: [Horarios, Disponibilidade, Bloqueios],
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
            ]),
            obter: vi.fn(),
            listarProfissionais: vi.fn(),
            carregando: signal(false),
            erro: signal(null),
          },
        },
        {
          provide: ConfiguracoesService,
          useValue: {
            horarios: signal(semana),
            disponibilidade: signal([
              { id: 'j1', diaSemana: 1, iniciaEm: '10:00:00', terminaEm: '12:00:00' },
            ]),
            bloqueios: signal([]),
            carregando: signal(false),
            erro: signal(null),
            sucesso: signal(null),
            listarHorarios: vi.fn(),
            salvarHorarios: vi.fn(),
            listarDisponibilidade: vi.fn(),
            salvarDisponibilidade: vi.fn(),
            listarBloqueios: vi.fn(),
            criarBloqueio: vi.fn(),
            removerBloqueio: vi.fn(),
            limparMensagens: vi.fn(),
          },
        },
      ],
    }).compileComponents();
  }

  async function renderizar(componente: Type<unknown>) {
    await configurar();
    const fixture = TestBed.createComponent(componente);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should show the seven weekdays on opening hours', async () => {
    const elemento = await renderizar(Horarios);
    expect(elemento.textContent).toContain('Segunda');
    expect(elemento.textContent).toContain('Domingo');
    expect(elemento.textContent).toContain('Editar horários');
  });

  it('should show availability form for the owner', async () => {
    const elemento = await renderizar(Disponibilidade);
    expect(elemento.querySelector('select#profissional')).not.toBeNull();
    expect(elemento.textContent).toContain('Salvar disponibilidade');
  });

  it('should show empty state for blocks', async () => {
    const elemento = await renderizar(Bloqueios);
    expect(elemento.textContent).toContain('Nenhum bloqueio');
    expect(elemento.textContent).toContain('Adicionar bloqueio');
  });

  it('should edit, cancel and save opening hours', async () => {
    await configurar();
    const fixture = TestBed.createComponent(Horarios);
    const componente = fixture.componentInstance;
    const servico = TestBed.inject(ConfiguracoesService);
    const salvar = vi.mocked(servico.salvarHorarios);
    await fixture.whenStable();

    componente.iniciarEdicao();
    expect(componente.editando()).toBe(true);
    expect(componente.rascunho()).toHaveLength(semana.length);
    expect(componente.rascunho()[0]).not.toBe(semana[0]);

    componente.alternarFechado(1, true);
    expect(componente.rascunho()[0].fechado).toBe(true);
    componente.alternarFechado(99, true);
    expect(componente.rascunho().every((dia) => dia.diaSemana !== 99)).toBe(true);

    componente.definirHorario(7, 'abreAs', '08:00');
    componente.definirHorario(7, 'fechaAs', '');
    expect(componente.rascunho()[1].abreAs).toBe('08:00');
    expect(componente.rascunho()[1].fechaAs).toBeNull();

    expect(componente.horaTela('09:00:00')).toBe('09:00');
    expect(componente.horaTela(null)).toBe('');

    componente.salvar();
    expect(salvar).toHaveBeenCalledWith(
      'b1',
      [
        { diaSemana: 1, abreAs: '09:00:00', fechaAs: '18:00:00', fechado: true },
        { diaSemana: 7, abreAs: '08:00:00', fechaAs: null, fechado: true },
      ],
      expect.any(Function),
    );
    const concluido = salvar.mock.calls[0][2];
    concluido?.();
    expect(componente.editando()).toBe(false);
    expect(componente.rascunho()).toEqual([]);
  });

  it('should not request hours without an active barbearia', async () => {
    await configurar();
    const barbearias = TestBed.inject(BarbeariaService) as unknown as {
      barbeariaId: { set(valor: string | null): void };
    };
    barbearias.barbeariaId.set(null);

    const fixture = TestBed.createComponent(Horarios);
    await fixture.whenStable();

    expect(vi.mocked(TestBed.inject(ConfiguracoesService).listarHorarios)).not.toHaveBeenCalled();
    fixture.componentInstance.salvar();
    expect(vi.mocked(TestBed.inject(ConfiguracoesService).salvarHorarios)).not.toHaveBeenCalled();
  });

  it('should load the barbearia when only its id is known', async () => {
    await configurar();
    const barbearias = TestBed.inject(BarbeariaService) as unknown as {
      barbearia: { set(valor: unknown): void };
    };
    barbearias.barbearia.set(null);

    const fixture = TestBed.createComponent(Horarios);
    await fixture.whenStable();

    expect(vi.mocked(TestBed.inject(BarbeariaService).obter)).toHaveBeenCalledWith('b1');
    expect(vi.mocked(TestBed.inject(ConfiguracoesService).listarHorarios)).toHaveBeenCalledWith(
      'b1',
    );

    fixture.componentInstance.iniciarEdicao();
    fixture.componentInstance.definirHorario(99, 'abreAs', '10:00');
    expect(fixture.componentInstance.rascunho().every((dia) => dia.diaSemana !== 99)).toBe(true);

    fixture.componentInstance.cancelar();
    expect(fixture.componentInstance.editando()).toBe(false);
  });

  it('should not have critical axe violations on opening hours', async () => {
    expect(await violacoesCriticas(await renderizar(Horarios))).toEqual([]);
  });
});
