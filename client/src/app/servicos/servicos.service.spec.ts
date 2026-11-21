import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { ServicosService } from './servicos.service';

const servico = {
  id: 's1',
  barbeariaId: 'b1',
  nome: 'Corte',
  duracaoMinutos: 30,
  preco: 50,
  ativo: true,
  ehModelo: false,
};

describe('ServicosService', () => {
  let servicos: ServicosService;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    servicos = TestBed.inject(ServicosService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should list the catalog of the barbershop in the route', () => {
    servicos.listar('b1');

    const requisicao = http.expectOne('/api/v1/barbearias/b1/servicos');
    expect(requisicao.request.method).toBe('GET');
    requisicao.flush([servico]);
    expect(servicos.servicos()).toEqual([servico]);
  });

  it('should create a service and reload the catalog', () => {
    let concluiu = false;
    servicos.criar('b1', { nome: 'Corte', duracaoMinutos: 30, preco: 50 }, () => {
      concluiu = true;
    });

    const criacao = http.expectOne('/api/v1/barbearias/b1/servicos');
    expect(criacao.request.method).toBe('POST');
    criacao.flush(servico);
    http.expectOne('/api/v1/barbearias/b1/servicos').flush([servico]);
    expect(concluiu).toBe(true);
  });

  it('should logically remove a service through DELETE', () => {
    servicos.remover('b1', 's1');

    const remocao = http.expectOne('/api/v1/barbearias/b1/servicos/s1');
    expect(remocao.request.method).toBe('DELETE');
    remocao.flush(null);
    http.expectOne('/api/v1/barbearias/b1/servicos').flush([]);
    expect(servicos.servicos()).toEqual([]);
  });
});
