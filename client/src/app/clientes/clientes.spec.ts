import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { BarbeariaService } from '../barbearia/barbearia.service';
import { violacoesCriticas } from '../../testing/axe';
import { Clientes } from './clientes';
import { ClientesService } from './clientes.service';

const cliente = {
  id: 'u1',
  nomeCompleto: 'Cliente Alvo',
  email: 'cliente-alvo@exemplo.com',
  telefone: '11999999999',
};

describe('Serviço de clientes', () => {
  let servico: ClientesService;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    servico = TestBed.inject(ClientesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should list clients with tenant and pagination', () => {
    servico.listar('b1', 'Alvo', 2);
    const req = http.expectOne('/api/v1/barbearias/b1/customers?pagina=2&tamanho=20&busca=Alvo');
    expect(req.request.method).toBe('GET');
    req.flush({ itens: [cliente], total: 1, pagina: 2, tamanho: 20 });
    expect(servico.itens()).toEqual([cliente]);
    expect(servico.total()).toBe(1);
  });

  it('should fetch a related client', () => {
    servico.obter('b1', 'u1');
    const req = http.expectOne('/api/v1/barbearias/b1/customers/u1');
    req.flush(cliente);
    expect(servico.selecionado()).toEqual(cliente);
  });
});

describe('Tela administrativa de clientes', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Clientes],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: BarbeariaService,
          useValue: { barbeariaId: signal('b1'), obter: vi.fn(), listarProfissionais: vi.fn() },
        },
        {
          provide: ClientesService,
          useValue: {
            itens: signal([cliente]),
            selecionado: signal(cliente),
            total: signal(1),
            pagina: signal(1),
            carregando: signal(false),
            erro: signal(null),
            listar: vi.fn(),
            obter: vi.fn(),
            limpar: vi.fn(),
          },
        },
      ],
    }).compileComponents();
  });

  it('should render list, search and detail without management actions', async () => {
    const fixture = TestBed.createComponent(Clientes);
    await fixture.whenStable();
    const elemento = fixture.nativeElement as HTMLElement;

    expect(elemento.textContent).toContain('Cliente Alvo');
    expect(elemento.textContent).toContain('cliente-alvo@exemplo.com');
    expect(elemento.textContent).toContain('Dados do cliente');
    expect(elemento.textContent).toContain('Não há ações de cadastro, edição ou exclusão');
    expect(elemento.querySelector('input#clientes-busca')).not.toBeNull();
  });

  it('should not have critical axe violations', async () => {
    const fixture = TestBed.createComponent(Clientes);
    await fixture.whenStable();
    expect(await violacoesCriticas(fixture.nativeElement as HTMLElement)).toEqual([]);
  });
});
