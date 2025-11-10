#!/bin/bash

# Script de teste para os serviços de Moto
# Certifique-se de que a API está rodando em http://localhost:5294

API_URL="http://localhost:5294"
MOTO_ID=""

echo "=========================================="
echo "TESTANDO SERVIÇOS DE MOTO"
echo "=========================================="
echo ""

# Cores para output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Função para imprimir resultado
print_result() {
    if [ $1 -eq 0 ]; then
        echo -e "${GREEN}✓ Sucesso${NC}"
    else
        echo -e "${RED}✗ Erro${NC}"
    fi
    echo ""
}

# 1. TESTE: Listar todas as motos (GET /motos)
echo "1. Listando todas as motos (GET /motos)..."
RESPONSE=$(curl -s -w "\n%{http_code}" "${API_URL}/motos")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
echo "Status: $HTTP_CODE"
echo "Resposta: $BODY"
if [ "$HTTP_CODE" -eq 200 ]; then
    print_result 0
else
    print_result 1
fi

# 2. TESTE: Cadastrar uma nova moto (POST /motos)
echo "2. Cadastrando nova moto (POST /motos)..."
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "${API_URL}/motos" \
    -H "Content-Type: application/json" \
    -d '{
        "Ano": 2024,
        "Modelo": "Honda CB 600F",
        "Placa": "ABC1234"
    }')
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
echo "Status: $HTTP_CODE"
echo "Resposta: $BODY"
if [ "$HTTP_CODE" -eq 201 ]; then
    # Extrair o ID da moto criada
    MOTO_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
    echo "Moto criada com ID: $MOTO_ID"
    print_result 0
else
    print_result 1
fi

# 3. TESTE: Tentar cadastrar moto com placa duplicada (deve falhar)
echo "3. Tentando cadastrar moto com placa duplicada (deve retornar 409)..."
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "${API_URL}/motos" \
    -H "Content-Type: application/json" \
    -d '{
        "Ano": 2023,
        "Modelo": "Yamaha MT-07",
        "Placa": "ABC1234"
    }')
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
echo "Status: $HTTP_CODE"
echo "Resposta: $BODY"
if [ "$HTTP_CODE" -eq 409 ]; then
    print_result 0
else
    print_result 1
fi

# 4. TESTE: Buscar moto por ID (GET /motos/{id})
if [ -n "$MOTO_ID" ]; then
    echo "4. Buscando moto por ID (GET /motos/$MOTO_ID)..."
    RESPONSE=$(curl -s -w "\n%{http_code}" "${API_URL}/motos/${MOTO_ID}")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    BODY=$(echo "$RESPONSE" | sed '$d')
    echo "Status: $HTTP_CODE"
    echo "Resposta: $BODY"
    if [ "$HTTP_CODE" -eq 200 ]; then
        print_result 0
    else
        print_result 1
    fi
else
    echo -e "${YELLOW}4. Pulando teste - ID da moto não disponível${NC}"
    echo ""
fi

# 5. TESTE: Atualizar placa da moto (PUT /motos/{id}/placa)
if [ -n "$MOTO_ID" ]; then
    echo "5. Atualizando placa da moto (PUT /motos/$MOTO_ID/placa)..."
    RESPONSE=$(curl -s -w "\n%{http_code}" -X PUT "${API_URL}/motos/${MOTO_ID}/placa" \
        -H "Content-Type: application/json" \
        -d '{
            "Placa": "XYZ9876"
        }')
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    BODY=$(echo "$RESPONSE" | sed '$d')
    echo "Status: $HTTP_CODE"
    echo "Resposta: $BODY"
    if [ "$HTTP_CODE" -eq 200 ]; then
        print_result 0
    else
        print_result 1
    fi
else
    echo -e "${YELLOW}5. Pulando teste - ID da moto não disponível${NC}"
    echo ""
fi

# 6. TESTE: Listar todas as motos novamente (para verificar atualização)
echo "6. Listando todas as motos novamente (GET /motos)..."
RESPONSE=$(curl -s -w "\n%{http_code}" "${API_URL}/motos")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
echo "Status: $HTTP_CODE"
echo "Resposta: $BODY"
if [ "$HTTP_CODE" -eq 200 ]; then
    print_result 0
else
    print_result 1
fi

# 7. TESTE: Deletar moto (DELETE /motos/{id})
if [ -n "$MOTO_ID" ]; then
    echo "7. Deletando moto (DELETE /motos/$MOTO_ID)..."
    RESPONSE=$(curl -s -w "\n%{http_code}" -X DELETE "${API_URL}/motos/${MOTO_ID}")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    echo "Status: $HTTP_CODE"
    if [ "$HTTP_CODE" -eq 204 ]; then
        print_result 0
    else
        print_result 1
    fi
else
    echo -e "${YELLOW}7. Pulando teste - ID da moto não disponível${NC}"
    echo ""
fi

# 8. TESTE: Tentar buscar moto deletada (deve retornar 404)
if [ -n "$MOTO_ID" ]; then
    echo "8. Tentando buscar moto deletada (deve retornar 404)..."
    RESPONSE=$(curl -s -w "\n%{http_code}" "${API_URL}/motos/${MOTO_ID}")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    BODY=$(echo "$RESPONSE" | sed '$d')
    echo "Status: $HTTP_CODE"
    echo "Resposta: $BODY"
    if [ "$HTTP_CODE" -eq 404 ]; then
        print_result 0
    else
        print_result 1
    fi
else
    echo -e "${YELLOW}8. Pulando teste - ID da moto não disponível${NC}"
    echo ""
fi

echo "=========================================="
echo "TESTES CONCLUÍDOS"
echo "=========================================="

