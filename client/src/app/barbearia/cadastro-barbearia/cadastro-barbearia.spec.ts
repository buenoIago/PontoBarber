import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../testing/axe';
import { CadastroBarbearia } from './cadastro-barbearia';

describe('CadastroBarbearia', () => {
  let fixture: ComponentFixture<CadastroBarbearia>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CadastroBarbearia],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CadastroBarbearia);
    http = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  it('should post the typed barbershop data and show the invite code', async () => {
    const elemento = fixture.nativeElement as HTMLElement;
    const valores: Record<string, string> = {
      'barbearia-nome': 'Barbearia Central',
      'barbearia-telefone': '(11) 99999-9999',
      'barbearia-email': 'contato@exemplo.com',
      'barbearia-cidade': 'São Paulo',
      'barbearia-endereco': 'Rua Central, 100',
    };

    for (const [id, valor] of Object.entries(valores)) {
      const input = elemento.querySelector<HTMLInputElement>(`input#${id}`)!;
      input.value = valor;
      input.dispatchEvent(new Event('input', { bubbles: true }));
    }

    fixture.detectChanges();
    await fixture.whenStable();

    elemento.querySelector('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const requisicao = http.expectOne('/api/v1/barbearias');
    expect((requisicao.request.body as { nome: string }).nome).toBe('Barbearia Central');
    requisicao.flush({
      id: 'b1',
      nome: 'Barbearia Central',
      codigoConvite: 'ABC23456',
      telefoneComercial: '(11) 99999-9999',
      emailComercial: 'contato@exemplo.com',
      cidade: 'São Paulo',
      endereco: 'Rua Central, 100',
      instagram: null,
      fusoHorarioId: 'America/Sao_Paulo',
    });
    await fixture.whenStable();

    expect(elemento.textContent).toContain('ABC23456');
    http.verify();
  });

  it('should not have critical axe violations', async () => {
    expect(await violacoesCriticas(fixture.nativeElement as HTMLElement)).toEqual([]);
  });
});
