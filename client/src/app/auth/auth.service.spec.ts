import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

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

  it('should post registration to the proper flow endpoint', () => {
    configurar();
    const dados = {
      nomeCompleto: 'Nome',
      email: 'novo@exemplo.com',
      telefone: '11999999999',
      senha: 'Senha123',
      confirmarSenha: 'Senha123',
    };

    servico.cadastrarCliente(dados);
    responder('POST', '/api/v1/auth/register/customer');
    http.expectOne('/api/v1/auth/me').flush({});

    servico.cadastrarProfissional(dados);
    responder('POST', '/api/v1/auth/register/professional');
    http.expectOne('/api/v1/auth/me').flush({});

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

  it('should clear the account on logout', () => {
    configurar();
    servico.carregarConta();
    responder('GET', '/api/v1/auth/me', {
      id: '1',
      nomeCompleto: 'N',
      email: 'e',
      tipo: 'Customer',
    });

    servico.sair();
    responder('POST', '/api/v1/auth/logout');

    expect(servico.conta()).toBeNull();
    http.verify();
  });
});
