import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { BarbeariaService } from './barbearia.service';

describe('BarbeariaService', () => {
  let servico: BarbeariaService;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    servico = TestBed.inject(BarbeariaService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should create a barbershop with the typed payload', () => {
    servico.criar({
      nome: 'Central',
      telefoneComercial: '(11) 99999-9999',
      emailComercial: 'contato@exemplo.com',
      cidade: 'São Paulo',
      endereco: 'Rua Um, 1',
      instagram: null,
      fusoHorarioId: 'America/Sao_Paulo',
    });

    const requisicao = http.expectOne('/api/v1/barbearias');
    expect(requisicao.request.method).toBe('POST');
    expect((requisicao.request.body as { nome: string }).nome).toBe('Central');
    requisicao.flush({ id: 'b1', codigoConvite: 'ABC23456' });

    expect(servico.barbearia()?.id).toBe('b1');
    expect(servico.barbeariaId()).toBe('b1');
  });

  it('should load the client-facing scheduling data without management links', () => {
    servico.obterDadosAgendamentoPublico('b1');

    const requisicao = http.expectOne('/api/v1/barbearias/b1/agendamento-publico');
    expect(requisicao.request.method).toBe('GET');
    requisicao.flush({
      barbeariaId: 'b1',
      nomeBarbearia: 'Central',
      fusoHorarioId: 'America/Sao_Paulo',
      profissionais: [{ id: 'p1', nomeCompleto: 'Profissional' }],
    });

    expect(servico.barbeariaId()).toBe('b1');
    expect(servico.dadosPublicos()?.profissionais[0].id).toBe('p1');
    expect(servico.dadosPublicos()?.fusoHorarioId).toBe('America/Sao_Paulo');
  });

  it('should restore one active link automatically', () => {
    servico.restaurarVinculos([
      {
        id: 'v1',
        barbeariaId: 'b1',
        profissionalId: 'p1',
        cargo: 'Barbeiro',
        situacao: 'Ativo',
      },
      {
        id: 'v2',
        barbeariaId: 'b2',
        profissionalId: 'p2',
        cargo: 'Barbeiro',
        situacao: 'Inativo',
      },
    ]);

    expect(servico.barbeariaId()).toBe('b1');
    expect(servico.vinculo()?.profissionalId).toBe('p1');
    expect(servico.vinculosDisponiveis()).toHaveLength(1);
  });

  it('should leave the tenant unselected when several links are active', () => {
    const vinculos = [
      {
        id: 'v1',
        barbeariaId: 'b1',
        profissionalId: 'p1',
        cargo: 'Proprietario',
        situacao: 'Ativo',
      },
      {
        id: 'v2',
        barbeariaId: 'b2',
        profissionalId: 'p2',
        cargo: 'Barbeiro',
        situacao: 'Ativo',
      },
    ];
    servico.restaurarVinculos(vinculos);

    expect(servico.barbeariaId()).toBeNull();
    expect(servico.vinculo()).toBeNull();
    expect(servico.vinculosDisponiveis()).toHaveLength(2);

    servico.selecionarVinculo(vinculos[1]);
    expect(servico.barbeariaId()).toBe('b2');
    expect(servico.vinculo()?.profissionalId).toBe('p2');
  });

  it('should clear restored links on logout', () => {
    servico.restaurarVinculos([
      {
        id: 'v1',
        barbeariaId: 'b1',
        profissionalId: 'p1',
        cargo: 'Barbeiro',
        situacao: 'Ativo',
      },
    ]);

    servico.limpar();

    expect(servico.barbeariaId()).toBeNull();
    expect(servico.vinculo()).toBeNull();
    expect(servico.vinculosDisponiveis()).toEqual([]);
  });

  it('should join by invite code and load the professionals', () => {
    servico.entrar('ABC23456');

    const entrada = http.expectOne('/api/v1/barbearias/entrar');
    expect(entrada.request.body).toEqual({ codigoConvite: 'ABC23456' });
    entrada.flush({
      id: 'v1',
      barbeariaId: 'b1',
      profissionalId: 'p1',
      cargo: 'Barbeiro',
      situacao: 'Ativo',
    });

    http.expectOne('/api/v1/barbearias/b1/profissionais').flush([]);
    expect(servico.barbeariaId()).toBe('b1');
  });

  it('should deactivate a professional through the status endpoint', () => {
    servico.alterarStatus('b1', 'p1', false);

    const alteracao = http.expectOne('/api/v1/barbearias/b1/profissionais/p1/status');
    expect(alteracao.request.method).toBe('PATCH');
    expect(alteracao.request.body).toEqual({ ativo: false });
    alteracao.flush({ id: 'p1', nomeCompleto: 'Nome', telefone: null, situacao: 'Inativo' });

    http.expectOne('/api/v1/barbearias/b1/profissionais').flush([]);
  });
});
