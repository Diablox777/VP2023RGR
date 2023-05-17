using LogicSimulator.Models;
using ReactiveUI;

namespace LogicSimulator.ViewModels {
    public class ViewModelBase: ReactiveObject {
        public readonly static Mapper map = new(); // Объявляем объект Mapper для использования во всех ViewModel
        private static Project? current_proj; // Статическое поле для хранения текущего проекта
        protected static Project? CurrentProj { // При установке проекта также устанавливаем текущую схему проекта
            get => current_proj;
            set {
                if (value == null) return; // Если переданный проект равен null, завершаем установку
                current_proj = value; // Устанавливаем текущий проект
                map.current_scheme = value.GetFirstScheme(); // Устанавливаем текущую схему проекта
            }
        }

        /*
         * Для тестирования
         */

        public static Project? TopSecretGetProj() => current_proj; // Метод для получения текущего проекта (для тестирования)
    }
}