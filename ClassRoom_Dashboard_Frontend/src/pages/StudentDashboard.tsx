import React, { useEffect, useState } from "react";
import { useAuth } from "../contexts/AuthContext";
import apiClient from "../utils/apiClient";
import type {
  StudentDashboard as StudentDashboardData,
  Course,
  Coursework,
} from "../types/api";
import {
  ChartBarIcon,
  BookOpenIcon,
  ClipboardDocumentCheckIcon,
  ClockIcon,
  ExclamationTriangleIcon,
  CheckCircleIcon,
} from "@heroicons/react/24/outline";

const StudentDashboard: React.FC = () => {
  const { user } = useAuth();
  const [dashboardData, setDashboardData] =
    useState<StudentDashboardData | null>(null);
  const [courses, setCourses] = useState<Course[]>([]);
  const [recentAssignments, setRecentAssignments] = useState<Coursework[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        const [dashboardResponse, coursesResponse] = await Promise.all([
          apiClient.getStudentDashboard(),
          apiClient.getCourses(),
        ]);

        setDashboardData(dashboardResponse.data);
        setCourses(coursesResponse.data);

        const allAssignments: Coursework[] = [];
        for (const course of coursesResponse.data) {
          try {
            const assignmentsResponse = await apiClient.getCoursework(
              course.id
            );
            allAssignments.push(...assignmentsResponse.data);
          } catch (error) {
            console.error(
              `Failed to fetch assignments for course ${course.id}:`,
              error
            );
          }
        }

        const sortedAssignments = allAssignments
          .filter((a) => a.dueDate)
          .sort(
            (a, b) =>
              new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime()
          )
          .slice(0, 5);

        setRecentAssignments(sortedAssignments);
      } catch (error) {
        console.error("Failed to fetch dashboard data:", error);
      } finally {
        setLoading(false);
      }
    };

    if (user?.role === "alumno") {
      fetchDashboardData();
    }
  }, [user]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 dark:border-blue-400"></div>
      </div>
    );
  }

  if (!dashboardData) {
    return (
      <div className="text-center py-12 text-gray-500 dark:text-gray-400">
        No dashboard data available
      </div>
    );
  }

  const completionRate =
    dashboardData.totalSubmissions > 0
      ? (dashboardData.handedIn / dashboardData.totalSubmissions) * 100
      : 0;

  return (
    <div className="space-y-6 bg-gray-50 dark:bg-gray-900 min-h-screen p-6">
      {/* Welcome Header */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
          Welcome back, {user?.name || user?.email}!
        </h1>
        <p className="mt-1 text-gray-600 dark:text-gray-300">
          Here's your academic progress overview
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {[
          {
            label: "Total Assignments",
            value: dashboardData.totalSubmissions,
            icon: ChartBarIcon,
            iconColor: "text-blue-600 dark:text-blue-400",
          },
          {
            label: "Completed",
            value: dashboardData.handedIn,
            icon: CheckCircleIcon,
            iconColor: "text-green-600 dark:text-green-400",
          },
          {
            label: "Pending",
            value: dashboardData.pending,
            icon: ClockIcon,
            iconColor: "text-yellow-600 dark:text-yellow-400",
          },
          {
            label: "Late",
            value: dashboardData.late,
            icon: ExclamationTriangleIcon,
            iconColor: "text-red-600 dark:text-red-400",
          },
        ].map((card) => (
          <div
            key={card.label}
            className="bg-white dark:bg-gray-800 rounded-lg shadow p-6"
          >
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <card.icon className={`h-8 w-8 ${card.iconColor}`} />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-600 dark:text-gray-300">
                  {card.label}
                </p>
                <p className="text-2xl font-bold text-gray-900 dark:text-white">
                  {card.value}
                </p>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Progress Overview */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
            Completion Rate
          </h3>
          <div className="relative">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-gray-600 dark:text-gray-300">
                Progress
              </span>
              <span className="text-sm font-medium text-gray-900 dark:text-white">
                {completionRate.toFixed(1)}%
              </span>
            </div>
            <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
              <div
                className="h-2 rounded-full transition-all duration-300 bg-blue-600 dark:bg-blue-400"
                style={{ width: `${completionRate}%` }}
              ></div>
            </div>
          </div>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
            Enrolled Courses
          </h3>
          <div className="space-y-3">
            {courses.slice(0, 3).map((course) => (
              <div
                key={course.id}
                className="flex items-center justify-between"
              >
                <div className="flex items-center">
                  <BookOpenIcon className="h-5 w-5 text-blue-600 dark:text-blue-400 mr-3" />
                  <div>
                    <p className="text-sm font-medium text-gray-900 dark:text-white">
                      {course.name}
                    </p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      {course.section}
                    </p>
                  </div>
                </div>
                <span
                  className={`px-2 py-1 text-xs rounded-full ${
                    course.courseState === "ACTIVE"
                      ? "bg-green-100 text-green-800 dark:bg-green-700 dark:text-green-100"
                      : "bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200"
                  }`}
                >
                  {course.courseState}
                </span>
              </div>
            ))}
            {courses.length > 3 && (
              <p className="text-sm text-gray-500 dark:text-gray-400 text-center pt-2">
                +{courses.length - 3} more courses
              </p>
            )}
          </div>
        </div>
      </div>

      {/* Recent Assignments */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
            Recent Assignments
          </h3>
        </div>
        <div className="divide-y divide-gray-200 dark:divide-gray-700">
          {recentAssignments.length > 0 ? (
            recentAssignments.map((assignment) => {
              const dueDate = new Date(assignment.dueDate!);
              const isOverdue = dueDate < new Date();
              const isDueSoon =
                dueDate < new Date(Date.now() + 24 * 60 * 60 * 1000);

              return (
                <div key={assignment.id} className="px-6 py-4">
                  <div className="flex items-center justify-between">
                    <div className="flex-1">
                      <h4 className="text-sm font-medium text-gray-900 dark:text-white">
                        {assignment.title}
                      </h4>
                      <p className="text-sm text-gray-500 dark:text-gray-300">
                        {assignment.description}
                      </p>
                      <div className="flex items-center mt-2 space-x-4">
                        <span className="text-xs text-gray-500 dark:text-gray-300">
                          Due: {dueDate.toLocaleDateString()}
                        </span>
                        {assignment.maxPoints && (
                          <span className="text-xs text-gray-500 dark:text-gray-300">
                            {assignment.maxPoints} points
                          </span>
                        )}
                      </div>
                    </div>
                    <div className="flex items-center space-x-2">
                      {isOverdue && (
                        <span className="px-2 py-1 text-xs rounded-full bg-red-100 text-red-800 dark:bg-red-700 dark:text-red-100">
                          Overdue
                        </span>
                      )}
                      {isDueSoon && !isOverdue && (
                        <span className="px-2 py-1 text-xs rounded-full bg-yellow-100 text-yellow-800 dark:bg-yellow-700 dark:text-yellow-100">
                          Due Soon
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              );
            })
          ) : (
            <div className="px-6 py-8 text-center text-gray-500 dark:text-gray-400">
              <ClipboardDocumentCheckIcon className="mx-auto h-12 w-12 text-gray-400 dark:text-gray-300" />
              <h3 className="mt-2 text-sm font-medium text-gray-900 dark:text-white">
                No assignments
              </h3>
              <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                Get started by enrolling in courses.
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default StudentDashboard;
