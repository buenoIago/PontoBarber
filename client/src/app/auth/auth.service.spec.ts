import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { BarbeariaService } from '../barbearia/barbearia.service';
import { interceptorErro } from '../core/http/interceptor-erro';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let servico: AuthService;
  let http: HttpTestingController;

  function configurar(): void {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([interceptorErro])),
        provideHttpClientTesting(),
      ],
    });
    servico = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  }

  function responder(metodo: string, url: string, corpo: Record<string, unknown> = {}): void {
    const requisicao = http.expectOne(url);
    expect(requisicao.request.method).toBe(metodo);
    expect(requisicao.request.withCredentials).toBe(true);
    requisicao.flush(corpo);
  }

  it('should load the authenticated account from /auth/me', () => {
    configurar();

    servico.carregarConta();
    responder('GET', '/api/v1/auth/me', {
      id: '1',
      nomeCompleto: 'Nome',
      email: 'usuario@exemplo.com',
      telefone: null,
      tipo: 'Customer',
    });

    expect(servico.conta()?.email).toBe('usuario@exemplo.com');
    http.verify();
  });

  it('should restore a single active link after loading the account', () => {
    configurar();
    const barbearias = TestBed.inject(BarbeariaService);

    servico.carregarConta();
    responder('GET', '/api/v1/auth/me', {
      id: '1',
      nomeCompleto: 'Nome',
      email: 'usuario@exemplo.com',
      telefone: null,
      tipo: 'Professional',
      vinculos: [
        {
          id: 'v1',
          barbeariaId: 'b1',
          profissionalId: 'p1',
          cargo: 'Barbeiro',
          situacao: 'Ativo',
        },
      ],
    });

    expect(barbearias.barbeariaId()).toBe('b1');
    expect(barbearias.vinculo()?.profissionalId).toBe('p1');
    http.verify();
  });

  it('should not choose arbitrarily when the account has several links', () => {
    configurar();
    const barbearias = TestBed.inject(BarbeariaService);

    servico.carregarConta();
    responder('GET', '/api/v1/auth/me', {
      id: '1',
      nomeCompleto: 'Nome',
      email: 'usuario@exemplo.com',
      telefone: null,
      tipo: 'Professional',
      vinculos: [
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
      ],
    });

    expect(barbearias.barbeariaId()).toBeNull();
    expect(barbearias.vinculo()).toBeNull();
    expect(barbearias.vinculosDisponiveis()).toHaveLength(2);
    http.verify();
  });

  it('should not restore links for a customer account', () => {
    configurar();
    const barbearias = TestBed.inject(BarbeariaService);

    servico.carregarConta();
    responder('GET', '/api/v1/auth/me', {
      id: '1',
      nomeCompleto: 'Nome',
      email: 'usuario@exemplo.com',
      telefone: null,
      tipo: 'Customer',
      vinculos: [],
    });

    expect(barbearias.barbeariaId()).toBeNull();
    expect(barbearias.vinculosDisponiveis()).toEqual([]);
    http.verify();
  });

  it('should post each profile registration to its own endpoint', () => {
    configurar();
    const cliente = {
      nomeCompleto: 'Nome',
      email: 'novo@exemplo.com',
      telefone: '11999999999',
    };
    const profissional = {
      ...cliente,
      senha: 'Senha123',
      confirmarSenha: 'Senha123',
    };

    servico.cadastrarCliente(cliente);
    const cadastroCliente = http.expectOne('/api/v1/auth/cadastro/cliente');
    expect(cadastroCliente.request.body).toEqual(cliente);
    cadastroCliente.flush(null, { status: 201, statusText: 'Created' });

    servico.cadastrarProfissional(profissional);
    const cadastroProfissional = http.expectOne('/api/v1/auth/cadastro/profissional');
    expect(cadastroProfissional.request.body).toEqual(profissional);
    cadastroProfissional.flush(null, { status: 201, statusText: 'Created' });

    http.verify();
  });

  it('should request and verify the customer OTP through dedicated endpoints', () => {
    configurar();

    servico.solicitarCodigo('cliente@exemplo.com');
    const solicitacao = http.expectOne('/api/v1/auth/otp/solicitar');
    expect(solicitacao.request.body).toEqual({ email: 'cliente@exemplo.com' });
    solicitacao.flush(null, { status: 202, statusText: 'Accepted' });
    expect(servico.emailOtp()).toBe('cliente@exemplo.com');

    servico.verificarCodigo({ email: 'cliente@exemplo.com', codigo: '123456' });
    const verificacao = http.expectOne('/api/v1/auth/otp/verificar');
    expect(verificacao.request.body).toEqual({ email: 'cliente@exemplo.com', codigo: '123456' });
    verificacao.flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne('/api/v1/auth/me').flush({
      id: '1',
      nomeCompleto: 'Nome',
      email: 'cliente@exemplo.com',
      telefone: null,
      tipo: 'Customer',
    });

    expect(servico.emailOtp()).toBeNull();
    expect(servico.conta()?.tipo).toBe('Customer');
    http.verify();
  });

  it('should not open the OTP step when the request fails', () => {
    configurar();

    servico.solicitarCodigo('inexistente@exemplo.com');
    http
      .expectOne('/api/v1/auth/otp/solicitar')
      .flush(
        { title: 'Erro', status: 500, detail: 'Falha', code: 'erro.interno', traceId: 't' },
        { status: 500, statusText: 'Internal Server Error' },
      );

    expect(servico.emailOtp()).toBeNull();
    expect(servico.erro()).toContain('Falha');
    http.verify();
  });

  it('should expose a readable message when the API rejects the request', () => {
    configurar();

    servico.entrar({ email: 'usuario@exemplo.com', senha: 'Senha123' });
    const requisicao = http.expectOne('/api/v1/auth/login');
    requisicao.flush(
      {
        title: 'Não autenticado',
        status: 401,
        detail: 'E-mail ou senha incorretos.',
        code: 'auth.invalid_credentials',
        traceId: 'trace-1',
      },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(servico.erro()).toContain('E-mail ou senha incorretos.');
    expect(servico.erro()).toContain('referência: trace-1');
    http.verify();
  });

  it('should clear the account and the tenant on logout', () => {
    configurar();
    const barbearias = TestBed.inject(BarbeariaService);
    servico.carregarConta();
    responder('GET', '/api/v1/auth/me', {
      id: '1',
      nomeCompleto: 'N',
      email: 'e',
      tipo: 'Professional',
      vinculos: [
        {
          id: 'v1',
          barbeariaId: 'b1',
          profissionalId: 'p1',
          cargo: 'Proprietario',
          situacao: 'Ativo',
        },
      ],
    });
    expect(barbearias.barbeariaId()).toBe('b1');

    servico.sair();
    responder('POST', '/api/v1/auth/logout');

    expect(servico.conta()).toBeNull();
    expect(barbearias.barbeariaId()).toBeNull();
    expect(barbearias.vinculo()).toBeNull();
    expect(barbearias.vinculosDisponiveis()).toEqual([]);
    http.verify();
  });
});
