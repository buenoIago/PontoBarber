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

  it('should not have critical axe violations on opening hours', async () => {
    expect(await violacoesCriticas(await renderizar(Horarios))).toEqual([]);
  });
});
