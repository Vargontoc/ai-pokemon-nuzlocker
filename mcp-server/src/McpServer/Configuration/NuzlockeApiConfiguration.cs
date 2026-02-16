namespace es.vargontoc.nuzlocke.ai.Configuration
{
    public static class NuzlockeApiConfiguration
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            
        }

        public static void ConfigureApp(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if(env.IsDevelopment())
            {   
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(e => {e.MapControllers(); });
        }
    }
}