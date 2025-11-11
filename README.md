# 🛒 uMarket - Plataforma de Comercio Estudiantil UPB

**uMarket** es una aplicación móvil diseñada para facilitar el comercio entre estudiantes de la Universidad Pontificia Bolivariana (UPB). Conecta a estudiantes que buscan productos o servicios con vendedores estudiantiles que ofrecen sus artículos dentro del campus universitario.

---

## 📱 Características Principales

### Para Estudiantes (Compradores)
- 🔍 **Explorar Vendedores Disponibles** - Visualiza una lista en tiempo real de vendedores activos en el campus
- 💬 **Chat en Tiempo Real** - Solicita conversaciones con vendedores para consultar sobre sus productos
- 📝 **Gestión de Conversaciones** - Mantén un historial de tus conversaciones con diferentes vendedores
- 🔐 **Autenticación Segura** - Registro e inicio de sesión con tu número de carnet institucional

### Para Vendedores
- 📢 **Publicar Disponibilidad** - Activa tu perfil de vendedor para aparecer en las búsquedas
- 📝 **Descripción de Productos** - Agrega una descripción de lo que estás vendiendo actualmente
- 💼 **Gestión de Solicitudes** - Acepta o rechaza solicitudes de chat de estudiantes interesados
- 💬 **Chat con Compradores** - Comunícate en tiempo real con potenciales clientes
- ✅ **Finalizar Conversaciones** - Cierra conversaciones cuando se complete una venta

### Características Técnicas
- ⚡ **Comunicación en Tiempo Real** - Utiliza SignalR para chat instantáneo y notificaciones
- 🔄 **Sincronización Automática** - Actualización automática de listas de vendedores disponibles
- 🌐 **API REST** - Backend robusto con ASP.NET Core
- 📊 **Base de Datos SQL** - Almacenamiento persistente con Azure SQL Database
- 🔒 **Seguridad JWT** - Autenticación basada en tokens seguros

---

## 🏗️ Arquitectura del Proyecto

```
uMarket/
├── uMarket/                          # Aplicación móvil .NET MAUI
│   ├── Pages/                        # Páginas de la aplicación
│   │   ├── LoginPage.xaml            # Inicio de sesión
│   │   ├── RegisterPage.xaml         # Registro de usuarios
│   │   ├── RoleSelectionPage.xaml    # Selección de rol (Estudiante/Vendedor)
│   │   ├── StudentMainPage.xaml      # Página principal para estudiantes
│   │   ├── SellerMainPage.xaml       # Página principal para vendedores
│   │   └── ChatPage.xaml             # Interfaz de chat
│   ├── Services/                     # Servicios de la aplicación
│   │   ├── AuthService.cs            # Autenticación
│   │   ├── HubService.cs             # Conexión SignalR
│   │   ├── ConversationsService.cs   # Gestión de conversaciones
│   │   ├── SellersService.cs         # Gestión de vendedores
│   │   └── NavigationService.cs      # Navegación entre páginas
│   └── Models/                       # Modelos de datos (DTOs)
│
└── uMarket.Server/                   # Backend API
    ├── Controllers/                  # Controladores REST API
    │   ├── AuthController.cs         # Autenticación y registro
    │   ├── SellersController.cs      # Gestión de vendedores
    │   └── ConversationsController.cs # Gestión de conversaciones
    ├── Hubs/                         # SignalR Hubs
    │   └── ChatHub.cs                # Hub de chat en tiempo real
    ├── Models/                       # Modelos de dominio
    │   ├── ApplicationUser.cs        # Usuario del sistema
    │   └── Chat/                     # Entidades de chat
    └── Data/                         # Contexto de base de datos
        └── ApplicationDbContext.cs
```

---

## 🚀 Requisitos Previos

Antes de ejecutar el proyecto, asegúrate de tener instalado:

### Para el Servidor (Backend)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) - Framework de desarrollo
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) o [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-editions-express) (versión 2019 o superior)
- [Azure Data Studio](https://azure.microsoft.com/products/data-studio/) o [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms) - Para gestionar la base de datos

### Para la Aplicación Móvil
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (versión 17.8 o superior) con las siguientes cargas de trabajo:
  - Desarrollo de aplicaciones móviles con .NET (MAUI)
  - Desarrollo de ASP.NET y web
- **Para Android:**
  - Android SDK (API 21 o superior)
  - Emulador de Android o dispositivo físico con modo de desarrollo habilitado
- **Para iOS:**
  - macOS con Xcode instalado
  - Dispositivo iOS o simulador

### Herramientas Opcionales (Recomendadas)
- [Postman](https://www.postman.com/downloads/) - Para probar la API REST
- [Git](https://git-scm.com/downloads) - Para control de versiones

---

## ⚙️ Configuración Inicial

### 1. Clonar el Repositorio

```bash
git clone https://github.com/abel8000001/uMarket.git
cd uMarket
```

### 2. Configurar la Base de Datos

#### Opción A: SQL Server Local (Recomendado para desarrollo)

1. **Crear la base de datos:**
   - Abre SQL Server Management Studio (SSMS) o Azure Data Studio
   - Conéctate a tu instancia local de SQL Server
   - Ejecuta el siguiente comando para crear la base de datos:

```sql
CREATE DATABASE uMarketDB;
GO
```

2. **Configurar la cadena de conexión:**
   - Navega a la carpeta del servidor: `cd uMarket.Server`
   - Crea un archivo `appsettings.Development.json` (si no existe):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=uMarketDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "tu-clave-secreta-de-al-menos-32-caracteres-aqui",
    "Issuer": "uMarket.Server",
    "Audience": "uMarket.Clients",
    "ExpiresMinutes": 60
  }
}
```

**Nota:** Si estás usando SQL Server con autenticación por usuario y contraseña, usa:
```json
"DefaultConnection": "Server=localhost;Database=uMarketDB;User ID=tu_usuario;Password=tu_contraseña;TrustServerCertificate=True;"
```

3. **Aplicar migraciones de base de datos:**

```bash
# Desde la carpeta uMarket.Server
dotnet ef database update
```

Si encuentras errores, asegúrate de tener instalado Entity Framework Core Tools:
```bash
dotnet tool install --global dotnet-ef
```

#### Opción B: Azure SQL Database (Para producción)

Si ya tienes una base de datos Azure SQL configurada, utiliza User Secrets:

1. Navega a la carpeta del servidor:
```bash
cd uMarket.Server
```

2. Inicializa User Secrets:
```bash
dotnet user-secrets init
```

3. Configura los secretos:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=tcp:tu-servidor.database.windows.net,1433;Initial Catalog=uMarket.db;User ID=tu_usuario;Password=tu_contraseña;Encrypt=true;Connection Timeout=30;"

dotnet user-secrets set "Jwt:Key" "tu-clave-secreta-de-al-menos-32-caracteres"
dotnet user-secrets set "Jwt:Issuer" "uMarket.Server"
dotnet user-secrets set "Jwt:Audience" "uMarket.Clients"
dotnet user-secrets set "Jwt:ExpiresMinutes" "60"
```

### 3. Crear Funciones y Procedimientos de Base de Datos (Opcional pero Recomendado)

Para mejorar el rendimiento, ejecuta estos scripts SQL en tu base de datos:

```sql
-- Función para obtener solicitudes de chat pendientes
CREATE FUNCTION dbo.tvf_GetPendingChatRequests(@UserId NVARCHAR(450))
RETURNS TABLE
AS
RETURN
(
    SELECT 
        cr.Id AS RequestId,
        cr.FromUserId,
        u.FullName AS FromFullName,
        u.UserName AS FromUserName,
        cr.CreatedAt
    FROM dbo.ChatRequests cr
    INNER JOIN dbo.AspNetUsers u ON cr.FromUserId = u.Id
    WHERE cr.ToUserId = @UserId 
      AND cr.Status = 0
);
GO

-- Procedimiento para aceptar solicitudes de chat
CREATE PROCEDURE dbo.sp_AcceptChatRequest
    @ChatRequestId UNIQUEIDENTIFIER,
    @AcceptedBy NVARCHAR(450)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        DECLARE @FromUserId NVARCHAR(MAX), @ToUserId NVARCHAR(450), @ConversationId UNIQUEIDENTIFIER;

        SELECT @FromUserId = FromUserId, @ToUserId = ToUserId
        FROM dbo.ChatRequests
        WHERE Id = @ChatRequestId;

        IF @FromUserId IS NULL
        BEGIN
            RAISERROR('ChatRequest not found', 16, 1);
            ROLLBACK TRAN;
            RETURN;
        END

        SET @ConversationId = NEWID();

        INSERT INTO dbo.Conversations (Id, Title, CreatedAt, IsClosed, ClosedAt)
        VALUES (@ConversationId, CONCAT('Conversación desde solicitud ', CONVERT(nvarchar(36), @ChatRequestId)), SYSDATETIMEOFFSET(), 0, NULL);

        INSERT INTO dbo.ConversationParticipants (ConversationId, UserId, Role)
        VALUES (@ConversationId, @FromUserId, 1);

        IF @ToUserId IS NOT NULL
        BEGIN
            INSERT INTO dbo.ConversationParticipants (ConversationId, UserId, Role)
            VALUES (@ConversationId, @ToUserId, 2);
        END

        UPDATE dbo.ChatRequests
        SET ConversationId = @ConversationId, Status = 2
        WHERE Id = @ChatRequestId;

        INSERT INTO dbo.Messages (Id, ConversationId, SenderId, Content, SentAt, IsSystem, Metadata)
        VALUES (NEWID(), @ConversationId, 'SYSTEM', 'Conversación creada desde solicitud de chat aceptada.', SYSDATETIMEOFFSET(), 1, CONCAT('ChatRequest:', CONVERT(nvarchar(36), @ChatRequestId)));

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRAN;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
    END CATCH
END;
GO
```

---

## 🖥️ Ejecutar el Servidor (Backend)

### Opción 1: Desde Visual Studio

1. Abre la solución `uMarket.sln` en Visual Studio 2022
2. Establece `uMarket.Server` como proyecto de inicio:
   - Clic derecho en `uMarket.Server` → **Establecer como proyecto de inicio**
3. Presiona **F5** o haz clic en el botón **▶ uMarket.Server** para ejecutar

El servidor se ejecutará en: `http://localhost:5000`

### Opción 2: Desde la Línea de Comandos

```bash
# Navega a la carpeta del servidor
cd uMarket.Server

# Restaurar dependencias
dotnet restore

# Ejecutar el servidor
dotnet run
```

### Verificar que el Servidor está Funcionando

Abre tu navegador y visita:
- **Swagger UI (solo en desarrollo):** `http://localhost:5000/swagger` (si está habilitado)
- **Endpoint de prueba:** `http://localhost:5000/debug/ping` (solo desarrollo)

Deberías ver una respuesta exitosa (204 No Content) o la interfaz de Swagger.

---

## 📱 Ejecutar la Aplicación Móvil

### Desde Visual Studio 2022

#### Para Android

1. **Configurar el emulador o dispositivo:**
   - **Emulador:** Asegúrate de tener un emulador Android configurado (Visual Studio → **Herramientas** → **Android** → **Administrador de dispositivos Android**)
   - **Dispositivo físico:** Habilita el modo de desarrollador y depuración USB en tu dispositivo Android

2. **Establecer proyecto de inicio:**
   - Clic derecho en `uMarket` (el proyecto MAUI) → **Establecer como proyecto de inicio**

3. **Seleccionar el destino:**
   - En la barra de herramientas superior, selecciona:
     - Framework: `net9.0-android`
     - Dispositivo: Tu emulador o dispositivo físico

4. **Importante: Configurar la dirección del servidor para Android**
   
   Si estás usando un **emulador Android**, necesitas cambiar la URL del servidor porque `localhost` no funciona en emuladores Android.

   Abre `uMarket\ApiSettings.cs` y cambia temporalmente:
   ```csharp
   #if DEBUG
       // Para emulador Android, usa 10.0.2.2 en lugar de localhost
       public static string BaseUrl { get; set; } = "http://10.0.2.2:5000/";
   #else
       // ...
   #endif
   ```

   **Nota:** 
   - `10.0.2.2` es la dirección especial que el emulador Android usa para acceder a `localhost` de tu máquina
   - Si usas un dispositivo físico, necesitas usar la IP de tu computadora en la red local (ej: `http://192.168.1.100:5000/`)

5. **Ejecutar la aplicación:**
   - Presiona **F5** o haz clic en el botón **▶ Android Emulator** o **▶ [Tu Dispositivo]**

#### Para iOS (Solo en macOS)

1. **Emparejar con Mac:**
   - Si estás en Windows, necesitas emparejar Visual Studio con un Mac que tenga Xcode instalado
   - **Herramientas** → **iOS** → **Emparejar con Mac**

2. **Seleccionar dispositivo iOS:**
   - Framework: `net9.0-ios`
   - Dispositivo: Simulador de iOS o dispositivo físico

3. **Ejecutar:**
   - Presiona **F5**

---

## 🧪 Flujo de Prueba de la Aplicación

### 1. Crear Usuarios de Prueba

#### Registrar un Estudiante
1. Abre la aplicación
2. Haz clic en **"¿No tienes cuenta? Regístrate"**
3. Completa el formulario:
   - **Carnet:** `1000001`
   - **Nombre Completo:** `Juan Estudiante`
   - **Contraseña:** `Student123`
   - **Rol:** `Estudiante`
4. Haz clic en **Registrarse**

#### Registrar un Vendedor
1. Cierra sesión (si es necesario)
2. Registra otro usuario:
   - **Carnet:** `2000001`
   - **Nombre Completo:** `María Vendedora`
   - **Contraseña:** `Seller123`
   - **Rol:** `Vendedor`

### 2. Probar Funcionalidad de Vendedor

1. **Inicia sesión** como vendedor (`2000001`)
2. **Activa tu disponibilidad:**
   - En el panel principal, activa el switch **"Disponible"**
   - Agrega una descripción: `"Vendo apuntes de Cálculo y libros usados"`
   - Haz clic en **Guardar**
3. **Espera solicitudes** de estudiantes

### 3. Probar Funcionalidad de Estudiante

1. **Cierra sesión** e inicia sesión como estudiante (`1000001`)
2. **Busca vendedores disponibles:**
   - Verás a "María Vendedora" en la lista
3. **Solicita chat:**
   - Haz clic en el botón de chat junto al vendedor
4. **Espera que el vendedor acepte** la solicitud

### 4. Probar el Chat en Tiempo Real

1. **Como vendedor:**
   - Ve a la pestaña **"Solicitudes Pendientes"**
   - Acepta la solicitud de "Juan Estudiante"
2. **Como estudiante:**
   - Ve a la pestaña **"Conversaciones"**
   - Abre la conversación con "María Vendedora"
3. **Envía mensajes** desde ambas cuentas (usa dos dispositivos o emuladores diferentes)
4. Observa que los mensajes aparecen en tiempo real

---

## 🔧 Solución de Problemas Comunes

### El servidor no inicia

**Error:** `JWT Key not configured`
- **Solución:** Verifica que hayas configurado correctamente `appsettings.Development.json` o User Secrets con la clave JWT.

**Error:** `Connection string 'DefaultConnection' not found`
- **Solución:** Asegúrate de que la cadena de conexión esté en `appsettings.Development.json` o en User Secrets.

### La aplicación no se conecta al servidor

**Error:** `Connection refused` o timeout
- **Android Emulador:** Usa `http://10.0.2.2:5000/` en lugar de `http://localhost:5000/`
- **Android Dispositivo Físico:** Usa la IP de tu computadora (ej: `http://192.168.1.100:5000/`)
- **Firewall:** Asegúrate de que el firewall de Windows permita conexiones en el puerto 5000

### Las migraciones de base de datos fallan

**Error:** `A network-related or instance-specific error`
- **Solución:** 
  1. Verifica que SQL Server esté ejecutándose
  2. Comprueba el nombre del servidor en la cadena de conexión
  3. Si usas `Trusted_Connection=True`, asegúrate de que tu usuario de Windows tenga permisos

**Error:** `The certificate chain was issued by an authority that is not trusted`
- **Solución:** Agrega `TrustServerCertificate=True;` a tu cadena de conexión

### El chat no funciona en tiempo real

- Verifica que el servidor esté ejecutándose
- Comprueba la consola de depuración para errores de conexión SignalR
- Asegúrate de que la URL del hub sea correcta: `http://tu-servidor:5000/hubs/chat`

---

## 🌐 Despliegue en Azure (Producción)

### Requisitos
- Cuenta de Azure (los estudiantes pueden obtener créditos gratuitos con [Azure for Students](https://azure.microsoft.com/free/students/))

### Recursos Necesarios en Azure
1. **Azure SQL Database** - Base de datos en la nube
2. **Azure App Service** - Hosting del backend API
3. (Opcional) **Azure Key Vault** - Almacenamiento seguro de secretos

### Pasos Generales

1. **Crear Azure SQL Database:**
   ```bash
   az sql db create --resource-group uMarket-RG --server umarket-db --name uMarket.db --service-objective S0
   ```

2. **Configurar App Service:**
   - Crear un App Service Plan
   - Crear una Web App
   - Configurar las variables de entorno (Connection String, JWT settings)

3. **Desplegar desde GitHub Actions:**
   - El repositorio ya incluye workflows de GitHub Actions
   - Configura los secretos necesarios en GitHub (Azure credentials, publish profile)

4. **Actualizar la aplicación móvil:**
   - Cambia la URL de producción en `ApiSettings.cs`
   - Recompila en modo **Release**

Consulta la documentación de Azure para más detalles sobre el despliegue.

---

## 🛡️ Seguridad

### Para Desarrollo Local
- Los secretos están en `appsettings.Development.json` (no se suben a Git - incluido en `.gitignore`)
- La clave JWT debe ser de al menos 32 caracteres

### Para Producción
- **Nunca** subas credenciales al repositorio
- Usa Azure Key Vault o variables de entorno
- Habilita HTTPS en producción
- Rota las claves JWT periódicamente

---

## 📚 Tecnologías Utilizadas

### Backend
- **ASP.NET Core 9.0** - Framework web
- **Entity Framework Core** - ORM para base de datos
- **SignalR** - Comunicación en tiempo real
- **ASP.NET Core Identity** - Gestión de usuarios y autenticación
- **JWT Bearer Authentication** - Tokens de autenticación
- **SQL Server / Azure SQL** - Base de datos relacional

### Frontend (Aplicación Móvil)
- **.NET MAUI** - Framework multiplataforma
- **C# 13** - Lenguaje de programación
- **XAML** - Diseño de interfaces
- **SignalR Client** - Cliente de chat en tiempo real
- **HttpClient** - Consumo de API REST
- **Secure Storage** - Almacenamiento seguro de tokens

### DevOps
- **GitHub Actions** - CI/CD automatizado
- **Azure App Service** - Hosting en la nube
- **Azure SQL Database** - Base de datos en la nube

---

## 👥 Contribuir

Este proyecto fue desarrollado como parte de un trabajo universitario en la UPB. Si deseas contribuir:

1. Haz fork del repositorio
2. Crea una rama para tu feature (`git checkout -b feature/nueva-funcionalidad`)
3. Commit tus cambios (`git commit -m 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Abre un Pull Request

---

## 📄 Licencia

Este proyecto es de código abierto y está disponible para fines educativos.

---

## 📞 Soporte

Si encuentras algún problema o tienes preguntas:

1. Revisa la sección de **Solución de Problemas** arriba
2. Busca en los [Issues del repositorio](https://github.com/abel8000001/uMarket/issues)
3. Crea un nuevo Issue si tu problema no está resuelto

---

## ✨ Características Futuras Planeadas

- [ ] Sistema de calificaciones para vendedores
- [ ] Búsqueda y filtrado de vendedores por categoría
- [ ] Notificaciones push
- [ ] Galería de fotos de productos
- [ ] Historial de pedidos
- [ ] Modo oscuro

---

**Desarrollado con ❤️ para la comunidad estudiantil de la UPB**

¿Listo para empezar? Sigue las instrucciones de configuración y ¡comienza a vender o comprar dentro del campus! 🚀
