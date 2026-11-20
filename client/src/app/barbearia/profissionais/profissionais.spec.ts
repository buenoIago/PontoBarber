import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../testing/axe';
import { BarbeariaService } from '../barbearia.service';
import { Profissionais } from './profissionais';

describe('Profissionais', () => {
  let fixture: ComponentFixture<Profissionais>;
  let http: HttpTestingController;
  let servico: BarbeariaService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Profissionais],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Profissionais);
    http = TestBed.inject(HttpTestingController);
    servico = TestBed.inject(BarbeariaService);
    await fixture.whenStable();
  });

  it('should show an empty state without a barbershop', () => {
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Nenhuma barbearia selecionada',
    );
  });

  function selecionarBarbearia(): void {
    servico.criar({
      nome: 'Central',
      telefoneComercial: '(11) 99999-9999',
      emailComercial: 'contato@exemplo.com',
      cidade: 'São Paulo',
      endereco: 'Rua Um, 1',
      instagram: null,
      fusoHorarioId: 'America/Sao_Paulo',
    });
    http.expectOne('/api/v1/barbearias').flush({ id: 'b1', codigoConvite: 'ABC23456' });
  }

  it('should deactivate a professional through the status endpoint', async () => {
    selecionarBarbearia();
    await fixture.whenStable();
    http
      .expectOne('/api/v1/barbearias/b1/profissionais')
      .flush([
        { id: 'p1', nomeCompleto: 'Profissional Um', telefone: '11999999999', situacao: 'Ativo' },
      ]);
    fixture.detectChanges();
    await fixture.whenStable();

    const elemento = fixture.nativeElement as HTMLElement;
    const linha = elemento.querySelector('tbody tr')!;
    const botao = Array.from(linha.querySelectorAll('button')).find((item) =>
      item.textContent?.includes('Desativar'),
    )!;
    botao.click();
    await fixture.whenStable();

    const alteracao = http.expectOne('/api/v1/barbearias/b1/profissionais/p1/status');
    expect(alteracao.request.method).toBe('PATCH');
    expect(alteracao.request.body).toEqual({ ativo: false });
    alteracao.flush({ id: 'p1', nomeCompleto: 'Profissional Um', situacao: 'Inativo' });
    http
      .expectOne('/api/v1/barbearias/b1/profissionais')
      .flush([
        { id: 'p1', nomeCompleto: 'Profissional Um', telefone: '11999999999', situacao: 'Inativo' },
      ]);
    fixture.detectChanges();
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Inativo');
    http.verify();
  });

  it('should not have critical axe violations with a loaded table', async () => {
    selecionarBarbearia();
    await fixture.whenStable();
    http.expectOne('/api/v1/barbearias/b1/profissionais').flush([]);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(await violacoesCriticas(fixture.nativeElement as HTMLElement)).toEqual([]);
  });
});
